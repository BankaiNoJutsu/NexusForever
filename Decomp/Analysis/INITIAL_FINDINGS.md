# Client64 Initial Reverse-Engineering Findings

Generated from the Ghidra headless project in `Decomp\Analysis\ghidra_projects`
and exports in `Decomp\Analysis\exports`.

## Export Coverage

| Binary | Functions | Strings | Durable labels | Interesting strings | Selected | Decompiled |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `Houston64.exe` | 25,129 | 29,460 | 7,508 | 6,317 | 0 | 0 |
| `StsConnLib64.MT.dll` | 4,522 | 10,893 | 371 | 3,406 | 863 | 40 |
| `WildStar64.exe` | 24,981 | 45,034 | 1,037 | 11,000 | 2,259 | 200 |

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

Seventh follow-up (F-001 token crypto pass, 2026-05-22):

- Refreshed `StsConnLib64.MT.dll` export-only run
  (`run_ghidra_analysis.ps1 -ExportOnly -Targets StsConnLib64.MT.dll
  -MaxDecompiledFunctions 400`).
- **Password/SRP path (implemented in NexusForever)** remains:
  `/Sts/Connect` -> `/Auth/LoginStart` (txn `0x16`) -> server `Reply/KeyData`
  (base64 SRP `s` + `B`) -> `/Auth/KeyData` (txn `0x15`, client `A` + `M1` in
  base64 `KeyData`) -> server `Reply/KeyData` (`M2`) -> STS Arc4 enabled ->
  `/Auth/LoginFinish` (txn `0x17`) and later token/account routes.
- **External/token RSA path (mapped, blocked)** is a separate handshake:
  - `StsConn_SendLoginTokenStart` (`180003d70`, txn `0x61` / `LoginTokenStart`)
    sends binary `ClientRand` in `Request`.
  - Server must answer (handler not fully decompiled) with binary
    `ServerRand`, `ServerPublicKey`, and `ServerSignature` in `Reply`; only
    `StsConn_SendTokenKeyData` reads those names.
  - `StsConn_ValidateTokenServerKeyMaterial` (`180012de0`) gates progress;
    failure sets conn error `4` (same pattern as missing XML).
  - `StsCrypt_CreateRsaClient` (`180037cb0`, `Services/Crypt/CptRsa.cpp`) plus
    vtable calls derive `PremasterSecret` from server key material and staged
    client state (`param_1+0xc0`, `param_1+0xa8`, `param_1+0xcc`).
  - `StsConn_SendTokenKeyData` (`18000a730`, txn `0x3b` / `TokenKeyData`) sends
    binary `PremasterSecret`, binary `AuthnToken`, optional `AuthProviderCode`,
    and `AppId` in `Request` (`selected_decompiled.c` ~758-815).
- **Prerequisite token acquisition (mapped, not implemented):**
  `StsConn_SendRequestToken` (`180004c40`, txn `0x20`) sends `UserId` + `AppId`;
  `StsConn_SendAssociateMyExternalAccount` sends `AuthnToken` upstream;
  `StsConn_OnAuthnTokenResponse` reads reply `AuthnToken` only.
- **Not verified / security blockers:** RSA key format and length, signature
  algorithm and trust anchor, exact premaster derivation, `TokenKeyData` server
  reply fields and post-handshake session/crypto transition, and any
  `LoginToken` (`0x36`) follow-on. No retail STS startup capture in-repo.
  **Do not implement** `/Auth/LoginTokenStart` or `/Auth/TokenKeyData` until
  Verified with captures plus decompile of `FUN_180012de0` and
  `CTokenKeyDataTxnNotify`.

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

Twelfth rapid-transport UI?send-chain follow-up implemented from this pass:

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
  `IsSelfSpell` branches, and `SpellService_LookupSpellWrapperById` at
  `1407a0fd0` is the wrapper hash lookup used by `IsFreeformTarget` after the type-`7`
  service lookup branch.
- Direct recheck of the labeled `SpellService_ResolveSpellWrapper` body now
  tightens that shared wrapper path further. Before falling back to
  `SpellService_LookupSpellWrapperById(param_1)`, it checks whether `param_3`
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

Spell broadcast registration follow-up from this pass:

- A full `InspectCodeAddress` decompile of the WildStar64 registration block
  `FUN_14006c290` superseded the earlier partial caller-window inference for
  this spell family.
- The corrected client registration block wires `0x0814 -> 140096000`,
  `0x0815 -> 140080d30`, `0x0816 -> 140095f30`, `0x0817 -> 140095fb0`,
  `0x0818 -> 140095e60`, and the previously conflicted
  `ServerSpellList_ReadPayload` reader actually sits at `0x0551 -> 140096060`.
- `140096000` reads one 18-bit `Spell4Id` plus one trailing 1-bit flag, which
  makes it the current best client parse match for the source-side
  `ServerSpellTriggerFlag` payload shape, but at opcode `0x0814`, not `0x0815`.
- `140080d30` is a shared non-function code stub that reads only one 18-bit
  `Spell4Id` into a 4-byte object, so the client-side `0x0815` payload is now
  structurally smaller than the current source `ServerSpellTriggerFlag` model.
- The full registration-block decompile now shows that `140080d30` is the
  existing shared `ServerUInt18_ReadPayload` stub, reused by non-spell server
  opcodes `0x00B0`, `0x0129` (`ServerUnlockMount`), and `0x01AE`
  (`ServerUnlockVanityPet`) in addition to `0x0815`. That is hard evidence that
  `0x0815` is a generic one-field `18-bit Spell4Id` payload, not a spell-only
  trigger-flag packet.
- `140095f30` validates `0x0816` as three 18-bit `Spell4Id` values followed by
  one 32-bit casting id, matching the current `ServerSpellHierarchy` model.
- `140095fb0` validates `0x0817` as one 18-bit `Spell4Id` plus one trailing
  byte, matching the current `ServerSpellEventByte` model.
- `140095e60` shows that `0x0818` reads one 32-bit leading field and then one
  `ServerSpellList_ReadTierEntry` structure at `+0x8`, so the current
  `ServerSpellTargetInfo` placeholder is now structurally conflicted.
- `TraceFunctionCallers` on `ServerSpellList_ReadTierEntry` (`140094aa0`) now
  shows `ServerOpcode0818_ReadLeadingUInt32AndTierEntry` as a direct caller
  alongside `ServerSpellList_ReadSpellEntry`, which ties `0x0818` into the
  spell-list tier/state helper family rather than the current `TargetInfo`
  helper assumption.
- `ServerSpellList_ReadTierEntry` field order is now explicit: `Spell4Id @ +0`,
  one byte at `+4`, one byte at `+5`, a 16-bit field at `+6`, a 4-bit field
  stored at `+8`, a variant-count byte at `+0xc`, and a variant-array pointer at
  `+0x10`.
- `ServerSpellList_ReadVariantEntry` now has two selector-specific tails behind
  its shared `19-bit effect id + three raw uint32 values + 2-bit selector`
  prefix. Selector `0` reads one `uint32`, one byte, three `uint32` values,
  and two trailing `uint32` values. Selector `1` reads one `uint16`, one
  `uint32`, one byte, three `uint32` values, and two trailing `uint32`
  values. The field semantics remain unresolved, but the current source-side
  spell-list placeholder no longer needs to stop at the selector.
- `AbilityBook_ApplySingleTierEntryDelta` (`1403b9410`) consumes exactly the
  first 12 bytes of that tier-entry structure as a compact single-entry
  spellbook delta record. It routes zero values in the 4-bit field stored at
  `+8` through `SpellBook_MarkSpellUnlearned` (`1403baea0`), nonzero values
  through `SpellBook_MarkSpellLearned` (`1403badb0`) when the spell is not
  already learned, uses `SpellBook_SetCurrentTierRank` (`1403bb200`) when the
  byte at `+4` is nonzero, retargets cached wrapper ids, and dispatches the
  named client event `AbilityBookChange`.
- The adjacent `0x017B` spell surface is now accounted for separately.
  Current-client reader `14008ee90` matches the tracked `ServerSpellUpdate`
  model exactly: `18-bit Spell4Id`, `4-bit tier`, `3-bit spec`, and one
  trailing activation bit. That explains the compact spell activation or LAS
  toggle update and removes `0x017B` as a candidate explanation for `0x0818`.
- The same state family is now tied to visible `AbilityBook` Lua surfaces.
  `Lua_AbilityBook_UpdateSpellTier` reads the cached pending tier-budget field
  at `+0x6ddc`, falls back to committed budget `+0x6dd8`, walks tier costs,
  and calls `SpellBook_SendTierUpdateRequest`. `Lua_AbilityBook_ClearCachedLASUpdates`
  clears the pending spell-tier queue at `+0x1458` and resets `+0x6ddc`.
- `SpellBook_SendTierUpdateRequest` confirms those fields are the spell-tier
  affordability block, not generic UI counters: it initializes `+0x6ddc` from
  committed budget `+0x6dd8`, applies the local cost delta, and caps the result
  at maximum budget `+0x6de0` before sending the tier update request.
- Direct decompile of `SpellBook_SendTierUpdateRequest` and
  `ActionSet_SendPendingActionSetChanges` now shows the shared
  `+0x1458/+0x1460` cache is client-local pending spell-update state keyed by
  `Spell4Id`, not a server-issued token table. The tier helper inserts or
  updates packed `Spell4Id + requested tier` state there, and the action-set
  helper later walks the same tree to build opcode `0x00B1`
  `ClientRequestActionSetChanges` spell entries. This weakens any theory that
  the leading `0x0818` `uint32` is just an echo of a client-generated request
  token.
- `SpellBook_UpdateTierPointBudget` (`1403b95c0`) updates the same
  `+0x6dd8/+0x6ddc/+0x6de0` block and dispatches `AbilityBookChange`, which
  strengthens the conclusion that the best current `0x0818` fit is a visible
  spellbook or ability-book delta rather than `TargetInfo`.
- `FUN_1403b77d0` still has a spellbook-facing subtype-`4` bulk-row branch.
  That path uses the row `Spell4Id @ +0x10` to call `1403ba550`, dispatches
  `AbilityBookChange` unless the trailing state at `+0xa8` is `0x31`, and then
  forwards the row `+0x18` index plus an optional resolved spell object through
  `140608c60`.
- `140608c60` first clears the indexed callback slot and then, when the
  supplied spell id matches an entry in `DAT_140c65898 + 0xa90`, reapplies the
  slot with a resolved object from `FUN_1405a4b80`. This makes the subtype-`4`
  row look like a visible ability-book slot or spell-object refresh path rather
  than `TargetInfo`.
- `1406089a0`, the sink underneath that path, stores or clears raw object
  pointers by the supplied `uint32` index inside an indexed pointer array and
  maintains sorted side lists from the same key. Because `140608c60` is only
  called from the subtype-`4` branch, the leading `0x0818` `uint32` now looks
  more like a slot or index candidate than a casting id.
- The richer `140569c90 -> 140569d30` constructor chain is now a ruled-out
  false lead for `0x0818`. `TraceFunctionCallers` and direct decompile show the
  same helpers are also used by guild-bank tab loaders `14057cdc0` and
  `14057d190`, so that object materializer is reusable client infrastructure,
  not packet-specific spell-tier evidence.
- This is the strongest current-client post-parse consumer candidate for
  `0x0818` found so far. It still does not prove a direct
  `0x0818 -> AbilityBook_ApplySingleTierEntryDelta` call and the leading
  32-bit field from `140095e60` remains unexplained, but it narrows `0x0818`
  away from `TargetInfo` and toward a single-entry ability-book or spellbook
  delta.
- A second independent repo-side source now aligns with the client mapping for
  `0x0814`, `0x0816`, and `0x0817`: the historical compare snapshot under
  `.nexusforever-runtime/compare-kirmmin-latest` defines
  `ServerSpellThresholdClear`, `ServerSpellThresholdStart`, and
  `ServerSpellThresholdUpdate` with payload shapes that exactly match the
  current client-proven readers, and the old `SpellThreshold.cs` runtime sent
  those packets from threshold finish/start/update transitions.
- That legacy threshold snapshot is specifically useful because the payload
  shapes line up with current client evidence: `ServerSpellThresholdClear`
  matches the current `0x0814` `Spell4Id + bool` reader, and
  `ServerSpellThresholdStart` / `ServerSpellThresholdUpdate` exactly match the
  current client-proven `0x0816` / `0x0817` readers.
- Hard client-consumer evidence now backs those semantics too. The current
  client `FUN_1403be940` wrapper forwards the parsed `0x0816` hierarchy
  payload (`Spell4Id`, two more 18-bit ids, and casting id) into
  `SpellThreshold_HandleStart` (`1403be620`), which allocates active threshold
  state, resolves stage metadata through the spell-service threshold cache, and
  dispatches the named client event `StartSpellThreshold`.
- The current client `SpellThreshold_HandleUpdate` (`1403bea90`) consumes the
  parsed `0x0817` `Spell4Id + byte` payload, stores that byte into the active
  threshold entry, dispatches `UpdateSpellThreshold`, and can emit
  `StartSpellThreshold` again when threshold presentation begins.
- The sibling clear path `SpellThreshold_HandleClear -> SpellThreshold_ClearActiveState`
  (`1403bed60 -> 1403bef30`) removes active threshold state for the same
  `Spell4Id`, which is the first direct client-consumer evidence that `0x0814`
  belongs to the same threshold lifecycle family even though the trailing flag
  meaning is still unresolved.
- Threshold state is confirmed to surface into user-facing client reads:
  `SpellWrapper_GetIconPathString` consults the active threshold cache to pick
  alternate icon assets, and `Lua_GameSpell_GetThresholdTime` sums per-stage
  threshold durations from the same spell-service threshold map.
- The same legacy snapshot does not strengthen `0x0818`: it keeps `0x0818` as
  a generic `TargetInfo` placeholder model, but no `Server0818` send sites were
  found there, so the historical source does not rescue the current
  `ServerSpellTargetInfo` assumption.
- Repo-side source evidence aligns with that correction: the current
  `ServerSpell0815` model already carries only one `Spell4Id`, matching the
  client stub exactly, and there are no live source send sites for
  `ServerSpellTriggerFlag`, `ServerSpell0815`, or `ServerSpellTargetInfo`.
- No runtime or packet-model change was made in this pass. The next safe step
  is to capture sniff witnesses for `0x0814`, `0x0815`, and `0x0818` before
  moving any live source model bindings.
- Verification: per-target export-only helper passes using
  `InspectCodeAddress.java` against `14006c290`, `140080d30`, `140095e60`,
  `140095f30`, and `140095fb0`, plus `TraceFunctionCallers.java` against
  `140094aa0`, confirmed the corrected registration, helper reuse, and body
  shapes.
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
- Direct decompile finally moves `140899fd0` past the earlier "common audio
  worker" boundary. The function takes the resolved entry id from the selector
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
  work near `14039bf20` is no longer "find the next sender" but to decide
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
- Direct inspect now also bounds the scene-tick wrapper above the remaining
  `0x022B` caller path. `14053a210` snapshots or reconstructs the local actor
  position, caches the sampled coordinates into the scene owner block at
  `+0x6d10/+0x6d18`, applies distance thresholds from the current replay/action
  config at `*(param_1 + 0x6ce8) + 0x70`, and only dispatches `140565d40` after
  `14047d830` confirms the sampled position stays clear. The helper
  `14047d830` itself performs two collision or clearance probes through the
  scene-world interface at `DAT_140c65898 + 0x7248` and rejects obstructed or
  too-near samples. That is strong enough to treat the `SceneLifecycle_UpdateAndReplayQueuedStates -> 14053a210 -> 140565d40`
  chain as a scene replay proximity or visibility gate, but still not enough to
  rename `ServerCinematic022B` or `140565d40` semantically.
- Direct inspect of sibling helpers `1405654a0`, `140565600`, `140566020`, and
  `1405660e0` further narrows that boundary without naming the packet.
  `1405654a0` resolves a context entry from the tree at
  `DAT_140c65b70 + 0x760`, falls back to `140565600` when no child list is
  present, and otherwise aggregates per-entity contributions via
  `Entity_ResolveById` before returning an integer score. `140565600` gathers
  four slot-like values through `140566ad0/140566d40`, averages selected
  categories, and runs a `GameTable 0x2c8`-driven scaling curve plus per-entity
  coefficient lookup before returning a final score-like float. `140566020`
  appends a non-null pointer into the global list at `DAT_140c65b70 +
  0x800/+0x808`, while `1405660e0` removes one from that same list. Together
  they reinforce that this neighborhood belongs to action/config scoring and
  downstream collection rather than a cinematic-specific semantic boundary for
  `0x022B`.
- Direct inspect of the next outer siblings `140565410` and `140566240` keeps
  that boundary conservative rather than naming the packet. `140565410` only
  inserts a missing entry keyed by `*(param_2 + 0x10)` into the same tree at
  `DAT_140c65b70 + 0x760`, while `140566240` is a generic callback walker over
  bucketed linked lists. Both look like supporting container-management helpers
  around the same action/config data structures, not cinematic-specific
  semantics for `0x022B`.
- Caller tracing and direct inspect of those two outer helpers push the
  boundary farther outward without improving packet semantics. `140453d90`
  initializes a vtable-backed object, assigns a monotonic id from
  `DAT_140c1e664`, zeroes local links/fields, and immediately registers the
  object through `140565410`, so that side is constructor-plus-tree
  registration plumbing. `1403cbc80` calls `140566240` over
  `DAT_140c65b70 + 0x7a0` with callback `14055c760`, then runs follow-up update
  helpers `1403d40e0`, `1403d4180`, `1403d4210`, and `1403d4910`, so that side
  belongs to a broader manager tick/update path. Together they reinforce that
  the `140565410`/`140566240` band is shared manager infrastructure rather than
  a stable cinematic-specific naming boundary for `0x022B`.
- Direct inspect of callback `14055c760`, initializer `14053d1f0`, and creator
  `140561780` pushes that outer band one level farther from packet semantics.
  `14055c760` asks the current object for a key through a vtable method,
  resolves a matching tree entry, and calls back into the object at
  `*(*param_1 + 0x138)` with the matched node's `+0x80` field. `14053d1f0`
  populates a `0x390`-byte runtime object with spell-derived timing/count
  fields, resolves source and target entities, links itself into entity-side
  lists, and schedules callback `140546e60`. Its only code caller found so far,
  `140561780`, performs `SpellService_LookupSpellWrapperByWrapperId`,
  `SpellService_LookupSpellWrapperById`, and `SpellService_ResolveSpellWrapper`
  lookups, allocates the object through `14053c500`, then routes it into lists
  at `DAT_140c65b70 + 0x7a0/+0x7c8`. That makes this outer neighborhood look
  like spell-wrapper and runtime-object creation/update infrastructure rather
  than a stable cinematic-specific naming boundary for `0x022B`.
- Direct inspect of scheduled callback `140546e60` and caller tracing for
  creator `140561780` reinforce that spell-runtime boundary. `140546e60`
  iterates bucketed entries beneath `param_1[0x27]`, gates each candidate
  through `140542d90`, computes delay windows and bitmask-based offsets, resolves
  source/target entities, and allocates `0x140` follow-up work items through
  `140549b90`; it looks like runtime action or spell scheduling, not packet
  shaping. Caller tracing for `140561780` found named callers
  `SpellCast_ResolveTargetsAndValidate` and
  `ServiceToken_SendClientSpellCastWithServiceToken`, plus additional unnamed
  callers `140458fe0` and `1405a51a0`, which further ties the outer band to
  spell-cast resolution and service-token flows rather than cinematic setup for
  `0x022B`.
- A higher-level owner pass still folds back into the same conservative
  boundary rather than exposing a packet semantic. The strongest recovered path
  remains `SceneLifecycle_UpdateAndReplayQueuedStates -> 14053a210 ->
  140565d40`: `14053a210` snapshots local actor position into `+0x6d10/+0x6d18`,
  applies replay/action thresholds from `*(param_1 + 0x6ce8) + 0x70`, and only
  dispatches `140565d40` after `14047d830` confirms clearance. Combined with the
  spell-cast and service-token callers above `140561780`, that keeps `0x022B`
  on a broader scene replay plus spell-runtime gate boundary rather than giving
  a stable rename for the packet itself.
- A deeper owner pass above that spell-runtime band still did not produce a
  safe packet rename. `SpellCast_SendClientSpellCastState` and
  `SpellCast_ValidateAndDispatch` stay on the spell-cast state and dispatch
  side, while `140549b90` is only the `0x140`-byte follow-up work-item
  allocator fed by scheduled callback `140546e60`. That work-item path belongs
  to runtime action scheduling and does not match the small `uint32 + bool`
  payload shape of `0x022B`, so the best current boundary remains the combined
  scene replay plus spell-runtime gate rather than a cinematic-specific name.
- A client-side consumer pass also failed to uncover a dedicated packet
  handler. The shared reader `ServerUInt32AndFlag_ReadPayload` still only
  establishes the `uint32 + bool` shape, while the current coverage inventory
  reports no direct handler for `0x022B`. The strongest downstream path remains
  contextual consumption inside `SceneLifecycle_UpdateAndReplayQueuedStates ->
  14053a210 -> 140565d40`, and the nearby `0x022A/0x022B/0x022C/0x022D`
  references in `140565d40` stay on the `GameFormula` side rather than exposing
  packet semantics. That further supports treating `0x022B` as a small
  scene-replay gate shell instead of a standalone cinematic command with a safe
  specific name.
- Fresh inspect of `140576f90` and `1405770f0`, together with the durable
  labels on `SpellRouteEvent_EmitOrQueue` and `SpellRouteEvent_DispatchByKind`,
  closes another false lead below the shared reader. `140576f90` allocates an
  `EVNT`-tagged `0x180` node through `14071f9b0` for queued route-event kind
  `7`, while `1405770f0` allocates an `EVNT`-tagged `0x188` node through
  `14071fad0` for queued route-event kind `8`. Both are queue builders hanging
  off the spell-wrapper route-listener or route-event system, not dedicated
  `0x022B` consumers or client packet handlers.
- A fresh inspect of nearby helper `1405698c0` rules out one more false lead.
  That function clears a `0x28`-byte buffer, copies fields from
  `Spell4CastResult`, `Spell4Base`, and `Spell4Visual`, and derives four flag
  bits from the visual row. It belongs to spell metadata packing rather than to
  the scene replay or cinematic gate family, so it does not tighten the
  `0x022B` packet semantics further.
- Direct inspect of sibling helper `140565aa0` narrows the same family without
  naming the packet. The function scales two float inputs using fields at
  `param_2 + 0x6dc/+0x6e4`, gates special target types `0x14/0x17`, and lazily
  pulls threshold coefficients from `GameTable` ids `0x4f2` and `0x2c8` before
  producing a clamped or eased distance-like result. That keeps it on the
  formula-driven action-config side of the boundary rather than exposing a
  cinematic-specific semantic for `0x022B`.
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
  identity; `ServerTwoUInt32_ReadPayload` (`14007a040`, formerly the narrower
  `ServerAccountItemCooldownSet_ReadPayload`) reads opcode `0x0974` as two
  32-bit fields; and
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
  the claim request. A later focused pass mapped the gift helpers:
  `AccountItem_SendClientGiftPendingItemGroupToCharacter` (`140006d60`)
  sends opcode `0x03F2`, and
  `AccountItem_SendClientGiftPendingItemGroupToAccount` (`140006e50`) sends
  opcode `0x03F1`.
- `StorefrontLib_PurchaseOffer` (`1404f1150`) confirms the Lua-facing purchase
  method routes normal purchases separately from recipient/extra-target flows.
  No server purchase-result UI packet was mapped from the result strings yet.
- NexusForever now names and parses `ClientAccountItemReturnPendingItemGroup`
  as opcode `0x07C6`, rejects it with an account-operation result like
  pending-group claim, and resends the authoritative empty pending list. The
  `ServerAccountItemsPending`
  model was corrected from unknown scalar fields to the mapped pending-group
  identity layout, but the server only emits an empty list until pending group
  storage, gift semantics, and non-empty source/target identity meanings are
  verified.
- Account-item cooldown mutation was left blocked in this pass because the
  native receive packet was mapped but current generated static data only
  exposed `AccountItemCooldownGroupEntry.Id`. The later one-hundredth pass
  implements the old-branch-compatible persisted cooldown boundary for known
  groups `1..3` while leaving additional duration policy blocked.
- Verification: Ghidra export refresh with `-MaxDecompiledFunctions 430`
  applied 302 WildStar64 labels and selected the new account-item callbacks.
  `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-account-inventory-blocked/`
  succeeds with only the existing `Spline.formation` warning.

Houston account inventory table-registration follow-up mapped from this pass:

- `Houston64.exe` now has durable client DB registration labels for the static
  data tables that back the implemented account inventory/runtime account
  currency surface: `ClientDB_RegisterAccountCurrencyType` (`1400a75c0`),
  `ClientDB_RegisterAccountItem` (`1400a7a00`), and
  `ClientDB_RegisterAccountItemCooldownGroup` (`1400a7e40`).
- The labels are anchored by the same repeated Houston table-loader pattern as
  the existing `RealmDataCenter`, `WorldSocket`, and reward-rotation labels.
  The refreshed export places the three functions at
  `exports\Houston64.exe\selected_decompiled.c:7`,
  `selected_decompiled.c:105`, and `selected_decompiled.c:203`, with matching
  strings `AccountCurrencyType`, `AccountItem`, `AccountItemCooldownGroup`,
  `DB\AccountCurrencyType.tbl`, `DB\AccountItem.tbl`, and
  `DB\AccountItemCooldownGroup.tbl`.
- No NexusForever runtime change was added here. The account inventory,
  account currency, account-item grant, and persisted cooldown boundaries were
  already implemented in earlier passes; this pass records Houston-side table
  evidence and leaves duration/pending-group semantics blocked where the
  account-item notes above already name the missing native evidence.
- Verification: `run_ghidra_analysis.ps1 -ExportOnly -Targets Houston64.exe
  -MaxDecompiledFunctions 2400` applied 11 Houston labels with no missing or
  duplicate rows, and the new labels appear in `functions.csv`,
  `selected_reasons_summary.csv`, and `selected_decompiled.c`.

Houston achievement/action/archive table-registration follow-up mapped from
this pass:

- `Houston64.exe` now has durable labels for the next 13 client DB registration
  functions in the same table-loader family:
  `ClientDB_RegisterAchievement` (`1400a8280`),
  `ClientDB_RegisterAchievementCategory` (`1400a86c0`),
  `ClientDB_RegisterAchievementGroup` (`1400a8b00`),
  `ClientDB_RegisterAchievementSubGroup` (`1400a8f40`),
  `ClientDB_RegisterAchievementChecklist` (`1400a9380`),
  `ClientDB_RegisterAchievementText` (`1400a97c0`),
  `ClientDB_RegisterActionBarShortcutSet` (`1400a9c00`),
  `ClientDB_RegisterActionSlotPrereq` (`1400aa040`),
  `ClientDB_RegisterArchiveArticle` (`1400aa480`),
  `ClientDB_RegisterArchiveCategory` (`1400aa8c0`),
  `ClientDB_RegisterArchiveEntry` (`1400aad00`),
  `ClientDB_RegisterArchiveEntryUnlockRule` (`1400ab140`), and
  `ClientDB_RegisterArchiveLink` (`1400ab580`).
- Each label is backed by the exact display table string passed through the
  Houston registration path plus the matching `DB\*.tbl` descriptor. The
  refreshed selected export places the batch at
  `exports\Houston64.exe\selected_decompiled.c:301` through
  `selected_decompiled.c:1477`, and the table names line up with existing
  `Source\NexusForever.GameTable\Model` entry classes.
- No NexusForever runtime change was added. These registrations corroborate
  static-data ownership for systems that already have GameTable models and
  prior WildStar achievement/GalacticArchive behavior notes; implementation
  remains driven by the more specific runtime/packet/function evidence in
  those focused sections rather than by table registration alone.
- Verification: `run_ghidra_analysis.ps1 -ExportOnly -Targets Houston64.exe
  -MaxDecompiledFunctions 2400` applied 24 Houston labels with no missing or
  duplicate rows, and all 13 new labels appear in `functions.csv`,
  `selected_reasons_summary.csv`, and `selected_decompiled.c`.

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
- Relocation recovery follow-up implemented from this pass:
  a local retry showed `!teleport location 51734` could place a character at
  the combat projector without making the projector usable when the finish-area
  progression had not been re-evaluated after the server-side relocation.
  `Player.OnRelocate(...)` now re-runs the Rider's Reef tutorial area-objective
  recovery after loading for active starter/follow-up tutorial quests, then
  refreshes tutorial entity visibility and progression. This keeps the normal
  hoverboard course path intact while allowing GM/debug teleports and other
  server-side relocations to recover the same finish trigger state that
  entered-world recovery already handled.
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

Ninety-sixth loot granted/explosion packet-field delta pass:

- Packet-field evidence:
  the existing native labels prove the client-side roll/master-loot Lua path
  consumes `itemDrop`, `bIsMaster`, `nTimeLeft`, `tLooters`, and
  `tLootersOutOfRange`. The broader export also contains `bCanLoot`,
  `eLootItemType`, and multiple `bGranted` strings in the loot/item-drop band,
  while `kirmmin/latest` serializes loot items with `CanLoot`, roll state,
  explosion state, and a granted bit before `RollTime`/random data/master-list
  fields. This pass treats `Granted` as the portable old-branch packet field
  without removing the newer `OnlyMasterLootable` field used by the current
  group/master implementation.
- Runtime fixes:
  `NexusForever.Network.World.Message.Model.Loot.LootItem` now carries and
  writes a `Granted` bit. `LootInstance` can now build a granted notification
  view that includes already-delivered items instead of filtering them out.
  Loot bags now follow the old branch's packet path more closely: consume the
  bag, deliver all generated contents without individual grant packets, then
  send an explosion `ServerLootNotify` with granted loot entries. Immediate
  account-currency loot, including random Omnibit kills, now also sends an
  explosion/granted notify after applying the currency reward.
- Account-item packet delta:
  current opcodes now include `ServerAccountItemDelete = 0x097C`, with a
  one-field payload containing the account inventory id. Account-item claim/take
  removal sends this packet and no longer refreshes the full account-item list
  after every successful claim, matching the old branch's explicit delete path
  more closely.
- Account-currency shower note:
  the old branch split account-currency notify payloads into up to 50 granted
  loot entries for the visual shower. The current implementation keeps that
  packet shape but distributes the total amount across the entries so the
  notification amounts sum to the actual granted currency amount.
- Verification:
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore`
  succeeds with only the pre-existing `Spline.formation` warning. A normal
  WorldServer build is blocked by the live `NexusForever.WorldServer` process
  locking its bin output, but the same project builds successfully to isolated
  output after the account-item delete packet change with
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\loot-granted-delta\`.

Ninety-seventh mapped opcode enum follow-up implemented from this pass:

- This pass focused on extending
  `Source\NexusForever.Network\Message\GameMessageOpcode.cs` from already
  durable client evidence. A comparison of mapped `function_labels.csv` opcode
  mentions and direct `Network_SendOpcodePayloadHelper(...)` calls in
  `selected_decompiled.c` showed the selected native send-helper surface was
  missing five enum names: `0x0096`, `0x0098`, `0x009D`, `0x03F1`, and
  `0x03F2`. Other apparent gaps from the raw label scan were non-opcode
  constants or offsets such as client table ids, audio offsets, and result enum
  values.
- NexusForever opcode coverage implemented:
  `ClientActivateUnitCastPosition = 0x0096` names the mapped activate-unit
  family request with context token, two selector nibbles, target id, and world
  position; `ClientActivateUnitCastTarget = 0x0098` names the related
  activate-unit family request with context token and target field;
  `ClientCastSpellSelected = 0x009D` names the selected spell-cast variant with
  context token, selected entry id, target id, and world position; and the
  pending account-item gift boundary now has
  `ClientAccountItemGiftPendingItemGroupToAccount = 0x03F1` plus
  `ClientAccountItemGiftPendingItemGroupToCharacter = 0x03F2`.
- Evidence locations:
  the activate-unit and selected spell-cast sender labels are already selected
  at `selected_decompiled.c:8236`, `8495`, `8737`, and `9119`; the pending
  account-item gift helpers are selected at `selected_decompiled.c:158` and
  `215`. The source enum now records the new low opcode entries around
  `GameMessageOpcode.cs:17` and the account-item gift entries around
  `GameMessageOpcode.cs:385`.
- Still blocked:
  this pass names opcodes only. It does not add message models or handlers for
  `0x0096`, `0x0098`, `0x009D`, `0x03F1`, or `0x03F2`; the activate-unit and
  spell-cast variants need a dedicated receive-model pass before server-side
  parsing, and pending account-item gifting remains blocked on exact recipient
  and delivery semantics.
- Verification:
  a focused send-helper comparison now reports `missingSendHelperOpcodes=0` for
  the selected WildStar64 export. `dotnet build
  Source\NexusForever.Network\NexusForever.Network.csproj --no-restore -m:1
  -v minimal --nologo` succeeds with `0` warnings and `0` errors.

Ninety-eighth development-only data boundary and item-container loot pass:

- Runtime data boundary implemented from this pass:
  `wildstar_client`, `jabbithole`, and `nf_map_*` are now documented as
  development-only evidence/import inputs. Runtime code must consume only
  promoted server-owned tables, `.tbl` data, scripts, or code assets. A focused
  model test asserts that `WorldContext` does not map `nf_map_*`,
  `wildstar_client`, or `jabbithole` tables.
- Item-container runtime hardening:
  `IGlobalLootManager` now exposes `HasLoot(IItem)` for server-owned item loot
  availability, and item loot drops now return success/failure. Loot-bag use
  validates that the consumed item has a loaded runtime `item_loot` table before
  consuming the bag, keeping category-138 items without promoted loot data from
  disappearing silently.
- Promoted item-container test coverage:
  new tests cover the safe-import `item_container_map.csv` policy represented
  in runtime tables: a flat item-container `loot_group` with
  `minDrop = maxDrop = 1` grants exactly one contained item, even when multiple
  weighted rows roll.
- No new client labels were added:
  this pass did not need fresh decompile exports. It implements the already
  verified runtime boundary around the item-loot path and records the
  development/runtime data split for future mapping passes.

Ninety-ninth packed-send opcode enum extension pass:

- Send-helper sweep:
  the selected WildStar64 export now includes the alternate packed send helper
  at `Network_SendOpcodePayloadOrPackedHelper`. Tracing its direct callers
  exposed eight enum gaps or mismatches: group `0x0411`/`0x0412`, housing
  neighbor `0x0512`/`0x0513`/`0x0515`/`0x0518`, housing visit identity
  `0x052F`, and recruitment-guild `0x076E`.
- Recruitment correction:
  `ClientRecruitmentGuildGetDetailedGuildInfo` was corrected from `0x776E` to
  `0x076E`. A focused immediate scan found `0x076E` at
  `RecruitmentGuild_SendClientGetDetailedGuildInfo` and found no `0x776E`
  immediates in `WildStar64.exe`.
- Group opcode map:
  `GroupLib.GotoGroupInstance` sends `0x0411` with the current group id, and
  `GroupLib.SetInstanceDifficulty` sends `0x0412` with the current group id
  plus the requested difficulty. The enum now names these as
  `ClientGroupGotoGroupInstance` and `ClientGroupSetInstanceDifficulty`.
- Housing neighbor opcode map:
  the HousingLib method table maps `NeighborInviteByName` to `0x0512`,
  `NeighborEvict` to `0x0515`, `NeighborInviteAccept`/`NeighborInviteDecline`
  to the boolean response opcode `0x0513`, and `NeighborSetPermission` to
  `0x0518` with selected neighbor identity plus a `0..2` permission value.
  The selected-residence visit helper sends `0x052F` with only a residence
  identity, so the enum now names it `ClientHousingVisitResidence` while
  leaving the existing fuller `ClientHousingVisit = 0x0531` intact.
- Durable labels and export verification:
  `function_labels.csv` now labels the packed send helper, the group and
  recruitment wrappers, the housing event callbacks, the HousingLib neighbor
  methods, and the identity-only housing visit sender. Re-exporting
  `WildStar64.exe` with `-MaxDecompiledFunctions 620` applied `552` labels and
  selected the new labels in `selected_decompiled.c`.
- Remaining implementation boundary:
  this pass names opcodes only. It does not add message models or handlers for
  the newly named group or housing client packets because the server-side
  mutation semantics and exact receive models still need a focused packet-model
  pass.
- Verification:
  the selected send-helper comparison now reports no missing enum values for
  direct calls through `Network_SendOpcodePayloadHelper`,
  `Network_SendOpcodePayloadOrPackedHelper`, `FUN_140016010`, or
  `FUN_1400161d0`. `dotnet build
  Source\NexusForever.Network\NexusForever.Network.csproj --no-restore -m:1
  -v minimal --nologo` and `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` both succeed with `0` warnings and
  `0` errors.

One-hundredth account item cooldown/result delta pass:

- Client/account evidence:
  `ServerTwoUInt32_ReadPayload` at `14007a040` reads opcode `0x0974` as two
  32-bit fields: account item cooldown group and cooldown seconds. The generic
  helper was previously labelled `ServerAccountItemCooldownSet_ReadPayload`,
  and the client Lua enum registration also exposes
  `CodeEnumAccountOperation` and `CodeEnumAccountOperationResult`, and the old
  `kirmmin/latest` branch used opcode `0x0970` as `ServerAccountOperationResult`
  with 32-bit operation and result fields.
- Runtime implementation:
  NexusForever now has `ServerAccountOperationResult = 0x0970`, account
  operation/result enums, and a packet model that writes the two 32-bit fields.
  `AccountInventoryManager.TakeItem` sends operation results for success,
  invalid inventory items, already-claimed state, target-character mismatch,
  prerequisite failure, max entitlement/generic-unlock failures, and cooldown
  rejection instead of relying on generic errors for the verified take path.
- Cooldown implementation:
  auth DB model/migration support for `account_item_cooldown` was added.
  Account inventory load now includes persisted cooldowns, initializes missing
  groups from `AccountItemCooldownGroup.tbl`, saves cooldown state, sends
  nonzero initial cooldowns, checks cooldowns before claim, triggers known group
  durations from the old branch (`1`/`2` = 1800 seconds, `3` = 28800 seconds),
  and emits `ServerAccountItemCooldownSet` after triggering.
- Account-currency packet cleanup:
  the unused duplicate `ServerGrantAccountCurrency` class was removed so opcode
  `0x0967` has a single registered model, the already-used
  `ServerAccountCurrencyGrant` payload of `AccountCurrency`, `Unknown0`, and
  `Unknown1`.
- Still blocked at that point:
  pending account-item group claim/return, gifting, coupon handling, and
  retail cooldown durations for any groups beyond `1..3` needed focused native
  or sniff evidence before replacing generic fallback paths or adding more
  policy. A later focused pass mapped the gift request payloads and replaced
  the pending/gift generic-error fallbacks with account-operation results.
- Verification:
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore
  -m:1 -v minimal --nologo` succeeds with only the pre-existing
  `Spline.formation` warning. `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. A direct auth-project build initially hit a transient compiler output
  file lock, then succeeded with isolated output using
  `-p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\auth-cooldown\`.
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\account-cooldown-world\`
  succeeds with only the pre-existing `Spline.formation` warning. `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo` passes `3` tests.

One-hundred-first direct-send opcode enum extension pass:

- Send-helper sweep:
  a wider direct-send pass over the WildStar64 `Network_SendOpcodePayloadHelper`
  surface found eighteen additional client opcode gaps. The enum now names
  item context action `0x00B6`, resource conversion `0x00D7`, dash cast
  `0x00DE`, spell cast state `0x00E3`, movement fall state `0x00FB`/`0x00FC`,
  repair vendor `0x014C`/`0x0167`, spell toggle cast `0x017E`, vehicle embark
  `0x01B0`, CREDD exchange `0x0265`/`0x0267`/`0x0269`/`0x026B`/`0x03E7`,
  housing interior wallpaper update `0x050D`, generic map node request
  `0x07CF`, and stale Spline2 request `0x081D`.
- Durable labels:
  `function_labels.csv` adds `24` WildStar64 labels for the mapped senders and
  Lua/UI wrappers, including `Lua_GameLib_ConvertResource`,
  `DashCast_SendClientDashCast`, repair-vendor senders,
  `CREDDExchange_SendClientOrderSubmit`, `Lua_CREDDExchangeLib_GetCREDDHistory`,
  `Lua_GameResidence_PurchaseInteriorWallpaper`,
  `GenericMap_SendClientNodeRequestOrChoose`, and
  `Spline2_SendClientStaleSplineRequests`.
- Evidence notes:
  CREDD exchange table evidence maps `RequestExchangeInfo`, `CancelOrder`, and
  `GetCREDDHistory`; the submit path selects buy/sell opcodes from UI state.
  `Residence.PurchaseInteriorWallpaper` validates six `HousingWallpaperInfo`
  selections against slot flags before sending changed slots through `0x050D`;
  `Residence.RemoveInteriorWallpaper` restores a single slot to that slot's
  default wallpaper id. `GenericMapNodeChoose` sends `0x07CF` only when node
  data is missing, otherwise it raises the local `GenericFloater`.
  `0x081D` is tied to the Spline2/Spline2Node runtime cache and is kept as a
  conservative stale Spline2 id request.
- Remaining implementation boundary:
  this pass names opcodes only. It does not add message models or handlers for
  the newly named packets; the movement, repair, housing, generic-map, CREDD,
  and Spline2 payloads still need dedicated receive-model passes before
  server-side mutation.
- Verification:
  a clean CSV-only WildStar64 export applied `576` labels with `0` skipped or
  missing labels after a full Auto export refreshed `selected_decompiled.c` but
  returned a nonzero status after export. The direct helper trace reports
  `records=348`, `immediateRecords=242`, `uniqueImmediate=175`, and
  `missingImmediate=0`. A selected-decompile scan across the known send helpers
  reports `selectedHelperCalls=116`, `unique=97`, and `missing=0`. `dotnet
  build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. The normal Network project build hit a transient `VBCSCompiler` lock,
  then succeeded with `-p:UseSharedCompilation=false` and isolated
  `BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\opcode-extension-network\`.

One-hundred-second account item pending gift semantics pass:

- AccountItemLib method-table mapping:
  `DumpNearbyData` over `140b69860` tied `TakeAccountItem`,
  `GiftPendingItemGroupToCharacter`, and `GiftPendingItemGroupToAccount` to
  wrappers `1404e50f0`, `1404e5190`, and `1404e52d0`. Focused inspection
  showed the character gift wrapper resolves a target character identity and
  calls sender `140006d60`; the account gift wrapper resolves a target account
  field and calls sender `140006e50`.
- Gift packet payloads:
  opcode `0x03F2` now has durable writer label
  `ClientAccountItemGiftPendingItemGroupToCharacter_WritePayload`
  (`140080990`) and serializes a pending group wide string followed by target
  character identity. Opcode `0x03F1` now has durable writer label
  `ClientAccountItemGiftPendingItemGroupToAccount_WritePayload` (`1400a0550`)
  and serializes a pending group wide string, 64-bit target account field,
  32-bit field, and current-character identity.
- NexusForever implementation:
  `GameMessageOpcode` now uses the client-facing gift names. Network.World
  parses `ClientAccountItemGiftPendingItemGroupToCharacter` and
  `ClientAccountItemGiftPendingItemGroupToAccount`. WorldServer handles both
  gift requests as a non-mutating unsupported path by refreshing pending items
  and sending `ServerAccountOperationResult` with `Operation=GiftItem` and
  `Result=NoGifting`. Pending group claim/return fallbacks now also use
  account-operation results (`ClaimPending`/`ReturnPending` with
  `InvalidPendingItem`) instead of generic errors.
- Still blocked:
  coupon request/send semantics remain enum-only in the selected client
  evidence: `RedeemCoupon` and `InvalidCoupon` are visible as account
  operation/result names, but no request opcode or payload writer was mapped in
  this pass. Cooldown durations beyond old-branch groups `1`, `2`, and `3`
  remain blocked because `AccountItemCooldownGroup.tbl` exposes only `Id` in
  current generated table models, while `AccountItemEntry` only references the
  group id and the server packet carries seconds supplied by the server.
- Verification:
  a CSV-only WildStar64 export applied `581` labels with `0` skipped or
  missing labels, and the new account gift labels appear in `functions.csv` and
  `selected_reasons_summary.csv`. `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. The normal WorldServer output path was locked by a running
  `NexusForever.WorldServer` process, so the focused build was repeated with
  `-p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\account-item-gift-world\`
  and succeeds with only the pre-existing `Spline.formation` warning.

One-hundred-third CREDD exchange receive-model pass:

- Implemented safe CREDD client receive models:
  `ClientCREDDExchangeRequestInfo` (`0x0269`) and
  `ClientCREDDExchangeRequestHistory` (`0x03E7`) are zero-byte messages.
  `ClientCREDDExchangeCancelOrder` (`0x0267`) carries one 64-bit order id,
  matching the durable native label evidence from
  `Lua_CREDDExchangeLib_CancelOrder`.
- Runtime behavior:
  WorldServer now handles the three mapped CREDD requests as a non-mutating
  unsupported path. Info/history requests return
  `ServerAccountOperationResult` with `GetCREDDExchangeInfo` and
  `CREDDExchangeNotLoaded`; cancel-order returns `CancelCREDDOrder` with
  `CREDDExchangeNotLoaded`. This keeps the client request path parsed and
  explicitly rejected until a real CREDD exchange service exists.
- Still blocked:
  buy/sell order submit opcodes `0x0265` and `0x026B` remain enum-only. The
  selected client sender shows a four-field local payload split by buy/sell UI
  state, but the exact packet writer bit widths for the money and selector
  fields still need a focused writer-function pass before adding receive
  models.
- Coverage and verification:
  `Get-DecompCoverageSnapshot.ps1` now reports client opcode coverage as
  `235` implemented, `65` partial, and `26` missing. The three implemented
  CREDD rows are marked `handled` in `opcode_coverage_inventory.csv`.
  `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\credd-exchange-world\`
  succeeds with only the pre-existing `Spline.formation` warning.

One-hundred-fourth group instance receive-model pass:

- Native evidence:
  the existing durable WildStar64 labels
  `Lua_GroupLib_GotoGroupInstance` (`140744610`) and
  `Lua_GroupLib_SetInstanceDifficulty` (`140743f30`) map the two group
  instance client sends. `GotoGroupInstance` sends `0x0411` with the current
  group id after active-group checks. `SetInstanceDifficulty` sends `0x0412`
  with the current group id and requested difficulty value.
- NexusForever implementation:
  `ClientGroupGotoGroupInstance` now parses the 64-bit group id.
  `ClientGroupSetInstanceDifficulty` parses the 64-bit group id and the
  2-bit `WorldDifficulty`, matching the existing instance-settings packet
  encoding. WorldServer now has handlers for both messages. The goto request is
  logged as an unsupported group-instance transfer. The difficulty request is
  logged and rejected through `ServerGroupActionResult` with
  `ChangeSettingsFailed` until group instance settings have a real backing
  service.
- Still blocked:
  this pass does not implement teleporting to a group instance or mutating
  group difficulty. The native client evidence maps the receive payloads, but
  NexusForever still lacks a verified group-instance runtime path for those
  mutations.
- Coverage and verification:
  `Get-DecompCoverageSnapshot.ps1` now reports client opcode coverage as
  `237` implemented, `68` partial, and `21` missing. The two group instance
  rows are marked `handled` in `opcode_coverage_inventory.csv`. `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\group-instance-world\`
  succeeds with only the pre-existing `Spline.formation` warning.

One-hundred-fifth mapped small client request pass:

- Native evidence:
  existing durable WildStar64 labels map several low-risk client request
  packets. `ResourceConversion_SendClientConvertResource` (`1404ab040`) sends
  `0x00D7` with a conversion id, selector/source field, and 64-bit resource
  field after validating the selected conversion record. `DashCast_SendClientDashCast`
  (`14039eff0`) sends `0x00DE` with a single direction/state value after local
  dash-cast success handling. `Vendor_SendClientRepairItemVendor` (`1403c28f0`)
  and `Vendor_SendClientRepairAllItemsVendor` (`1403c2a20`) share opcode
  `0x014C` as a three-64-bit repair request, with the repair-all branch using a
  zero item identity and a calculated total cost. `Lua_GameLib_IsRepairVendor`
  (`14050ce40`) sends zero-byte `0x0167`. `GenericMap_SendClientNodeRequestOrChoose`
  (`1404d1e40`) sends `0x07CF` only when the requested generic-map node is not
  locally available; the node id matches the existing 14-bit generic-map packet
  encoding.
- NexusForever implementation:
  WorldServer now handles `ClientConvertResource`, `ClientDashCast`,
  `ClientRepairItemVendor`, and `ClientRepairVendorStatusRequest` as parsed
  diagnostic-only requests. Resource conversion validates that the conversion id
  exists before logging the unsupported path. `ClientGenericMapNodeRequest` now
  parses a 14-bit node id and responds with `ServerGenericMapNode` for known
  nodes, while unknown ids are logged and ignored. `ClientGenericMapNodeChosen`
  now has a conservative unsupported handler so the already-modeled client
  choice packet is no longer silently unhandled.
- Still blocked:
  item repair, resource conversion, and dash-cast server mutation remain
  blocked. The selected client evidence maps the payloads, but safe server-side
  repair cost application, conversion inventory/currency changes, and dash
  state authority need dedicated runtime validation before mutation.
- Coverage and verification:
  `Get-DecompCoverageSnapshot.ps1` now reports client opcode coverage as
  `243` implemented, `71` partial, and `12` missing. The six rows touched in
  this pass are marked `handled` in `opcode_coverage_inventory.csv`. `dotnet
  build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\mapped-small-requests-world\`
  succeeds with only the pre-existing `Spline.formation` warning.

One-hundred-sixth housing neighbor receive-model pass:

- Native evidence:
  existing durable WildStar64 labels map the housing neighbor client requests.
  `HousingEvent_SendClientNeighborInvite` (`1404b9db0`) and
  `Lua_HousingLib_NeighborInviteByName` (`140735eb0`) send `0x0512` with
  either a target residence identity or target player name. The matching evict
  senders `1404b9f10` and `140735fb0` send `0x0515` with the same target
  shape. `HousingEvent_SendClientNeighborInviteAccept`/`Decline`
  (`1404ba070`/`1404ba0a0`) and their Lua wrappers send `0x0513` with an
  accept value of `1` or `0`. `HousingEvent_SendClientNeighborSetPermission`
  (`1404ba0d0`) and `Lua_HousingLib_NeighborSetPermission` (`1407360b0`) send
  `0x0518` with the target identity/name and a permission value constrained by
  the client to `0..2`.
- NexusForever implementation:
  `ClientHousingNeighborInvite`, `ClientHousingNeighborEvict`, and
  `ClientHousingNeighborSetPermission` now parse target residence, target name,
  and permission where applicable. `ClientHousingNeighborInviteResponse` now
  has a WorldServer handler. The neighbor handlers log the mapped request data
  and reject invalid permission values above `2`, but do not mutate residence
  neighbor state.
- Still blocked:
  the server-side housing relationship model and interior wallpaper packet
  decode were completed in later passes; remaining blockers are the broader
  timing, inventory-debit, and visit-state semantics tracked by F-004.
- Coverage and verification:
  `Get-DecompCoverageSnapshot.ps1` now reports client opcode coverage as
  `248` implemented, `71` partial, and `7` missing. The four housing neighbor
  rows are marked `handled` in `opcode_coverage_inventory.csv`. `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\housing-neighbor-world\`
  succeeds with only the pre-existing `Spline.formation` warning.

One-hundred-seventh attribute point receive-model pass:

- Native evidence:
  `GetAttributePoints`, `ResetAttributePoints`, and `SpendAttributePoints`
  strings led back into the GameLib attribute point Lua methods. Direct caller
  trace plus `InspectCodeAddress` confirmed `1406febf0` as
  `Lua_GameLib_SpendAttributePoints`, which validates six Lua numeric
  arguments, copies them into a 24-byte stack payload, and sends opcode
  `0x017C`. The same inspection confirmed `1406fed20` as
  `Lua_GameLib_ResetAttributePoints`, which validates local player/world state
  and sends opcode `0x0150` with an empty payload. Both labels are now durable
  rows in `Decomp\Analysis\function_labels.csv`.
- NexusForever implementation:
  `ClientSpendAttributePoints` now reads six `uint` attribute point values for
  `ClientSpendAttributePoints` (`0x017C`), and
  `ClientResetAttributePoints` models the zero-byte reset request
  (`0x0150`). WorldServer has conservative diagnostic handlers for both
  requests so the packets are recognized and logged without applying character
  stat mutation.
- Still blocked:
  authoritative attribute allocation/refund behavior remains blocked on the
  server-side character stat and attribute-point runtime model. The client
  senders prove the wire shape, but not the validation rules, cost rules, or
  resulting `ServerAttributePoints` update payload.
- Coverage and verification:
  `run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -DecompileMode
  Auto` applies `583` labels and refreshes the WildStar selected decompile.
  `Get-DecompCoverageSnapshot.ps1` now reports client opcode coverage as
  `250` implemented, `72` partial, and `4` missing; the two attribute point
  rows are marked `handled` in `opcode_coverage_inventory.csv`. `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\attribute-points-world\`
  succeeds with only existing package-version warnings plus the existing
  `Spline.formation` warning.

One-hundred-eighth instance settings and AbilityBook mapping pass:

- Native evidence:
  `TraceStringReferences.java` on `SetInstanceSettings`,
  `ResetSingleInstance`, and `AbilityBook`, plus `DumpNearbyData.java` on
  `140b73130`, `140b73140`, and `140b75190`, resolved new method-table pairs.
  `InspectCodeAddress.java` then confirmed `Lua_GameLib_GetInstanceSettings`
  at `140701530`, `Lua_GameLib_SetInstanceSettings` at `140701650`,
  `Lua_GameLib_ResetSingleInstance` at `1407017d0`, and
  `Lua_GameLib_OnClosedInstanceSettings` at `140701800`.
- The instance-settings helpers split cleanly into one local query and three
  request paths: `GetInstanceSettings` returns `bWorldForcesLevelScaling` and
  `eWorldDifficulty` from the current world state; `SetInstanceSettings` sends
  opcode `0x0163` using the cached instance-portal id plus compact validated
  setting bits; `ResetSingleInstance` sends opcode `0x0153`; and
  `OnClosedInstanceSettings` conditionally sends opcode `0x00D2` after the
  dialog closes, then clears the cached portal selection.
- The adjacent ability table at `PTR_s_GetAbilitiesList_140b75160` maps
  `Lua_AbilityBook_GetAbilitiesList` (`1407478d0`),
  `Lua_AbilityBook_GetAbilityInfo` (`140747990`),
  `Lua_AbilityBook_CheckSpellActivateRequirements` (`140747a80`),
  `Lua_AbilityBook_ActivateSpell` (`140748190`),
  `Lua_AbilityBook_UpdateSpellTier` (`140748390`),
  `Lua_AbilityBook_ClearCachedLASUpdates` (`140748630`), and the registrar
  `Lua_RegisterAbilityBook` (`140749690`).
- `Lua_RegisterAbilityBook` calls
  `FUN_140057020(param_1,"AbilityBook",&PTR_s_GetAbilitiesList_140b75160)` and
  registers `CodeEnumSpecConstant`, `CodeEnumSpecError`, and
  `CodeEnumEldanAvailability`.
- `Lua_AbilityBook_ActivateSpell` validates the local player state, resolves the
  spell wrapper, checks the local activation/tier gates, and sends opcode
  `0x017A` with the spell id plus activation bit when the client-side checks
  pass.
- `Lua_AbilityBook_UpdateSpellTier` blocks on
  `ActionSet_CheckUpdateSpellInProgress` and uses local tier/currency gates
  before dispatching the client-side spell-tier update helper; it is not the
  `ClientCommitAmpSpec` path.
- NexusForever mapping status:
  the existing packet models already line up with this evidence.
  `ClientClosedInstanceSettings`, `ClientResetSingleInstance`,
  `ClientSetInstanceSettings`, and `ClientAbilityBookActivateSpell` did not need
  wire-shape changes in this pass. `Decomp\Analysis\function_labels.csv` now
  carries the 11 new durable labels above so future export-only and
  `InspectCodeAddress.java` passes surface those names directly in
  `functions.csv`, `selected_xrefs.csv`, and targeted decompile output.
- Still blocked:
  the trailing packed setting bits in `ClientSetInstanceSettings` remain only
  partially named from the Lua side, so server mutation should stay
  conservative. `ClientCommitAmpSpec` remains a separate follow-up, and the full
  return-table shapes for `GetAbilitiesList` / `GetAbilityInfo` are still wider
  than the immediate packet-mapping need.
- Coverage and verification:
  `run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ProjectLayout
  PerTarget -DecompileMode Auto` now applies `594` WildStar64 labels with `0`
  missing labels. The refreshed export carries the new names in
  `functions.csv` and `selected_xrefs.csv`, while
  `LATEST_COVERAGE_SUMMARY.md` remains at `250` implemented, `72` partial, and
  `4` missing client opcodes because this pass only extended the decompile map.

One-hundred-ninth remaining client missing-model pass:

- Native evidence:
  focused `FindImmediateInstructions.java` traces on `0x00FC`, `0x0265`,
  `0x026B`, and `0x050D` resolved the registered payload writers and payload
  sizes for the last missing client model rows. The shared packet registration
  block `FUN_14006c290` proved the exact row pairs for the three previously
  high-risk packets: `0x0265` uses semantic size `0x10` with advance helper
  `140082c00` and writer `ClientCREDDExchangeBuyOrderSubmit_WritePayload`
  (`140082c10`), `0x026B` uses semantic size `0x20` with advance helper
  `14009fb40` and writer `ClientCREDDExchangeSellOrderSubmit_WritePayload`
  (`14009fb50`), and `0x050D` uses semantic size `0x288` with advance helper
  `14009ddb0` and writer `ClientHousingInteriorWallpaperUpdate_WritePayload`
  (`14009ddd0`). The same pass also retained
  `ClientMovementFallLand_WritePayload` (`14008b880`) and the shared
  `Housing_WriteDecorInfoPayload` (`14009d0a0`) labels in the durable export.
- Payload mapping:
  `ClientMovementFallLand` (`0x00FC`) serializes one state bit followed by
  three raw 32-bit movement position fields. `ClientCREDDExchangeBuyOrderSubmit`
  (`0x0265`) serializes a 64-bit credit amount followed by one submit/selector
  bit; its paired advance helper increments by `0x41` bits. `ClientCREDDExchangeSellOrderSubmit`
  (`0x026B`) serializes an identity, a 64-bit credit amount, and the same
  submit/selector bit; its paired advance helper increments by `0x8f` bits.
  The housing interior wallpaper update (`0x050D`) serializes six 32-bit slot
  flags followed by six `DecorInfo`-style records through the shared housing
  decor writer. The writer emits the six slot flags first, then iterates six
  times through `Housing_WriteDecorInfoPayload` with a `0x68` semantic stride,
  while the paired advance helper adds `0xc0` bits for the slot flags and then
  `0x2bc` bits for each `DecorInfo` record.
- NexusForever implementation:
  Network.World now has receive models for all four remaining client enum-only
  opcodes, and the current CREDD / wallpaper shapes already match the recovered
  writer evidence. WorldServer handles CREDD buy/sell with
  `ServerAccountOperationResult` and `CREDDExchangeNotLoaded`, movement
  fall-land remains logged only, and housing interior wallpaper updates now
  route through the residence map instead of diagnostic-only logging.
  `DecorType` value `3` is mapped as `InteriorWallpaper`; residence decor now
  persists the native `DecorData`, `HookBagIndex`, `HookIndex`, and
  `ActivePropUnitId` fields needed to round-trip the shared `DecorInfo` record.
  `ClientHousingInteriorWallpaperUpdate` treats the first six uint32 values as
  existing-decor flags, then applies each non-empty slot as an
  `InteriorWallpaper` decor record with hook indices `1..6`, creating or
  updating residence decor rows and echoing changed records through
  `ServerHousingResidenceDecor`.
- Still blocked:
  CREDD exchange persistence/matching, authoritative landing-state movement
  handling, and exact interior wallpaper ownership/unlock/refund semantics
  remain blocked on server-side runtime evidence. The residence visual/decor
  mutation and mapped `HousingWallpaperInfo` currency debit path are now
  implemented from the mapped `0x050D` payload.
- Coverage and verification:
  WildStar64 export-only Auto applies `599` source-controlled labels with `0`
  missing labels, and all five new labels appear in both `functions.csv` and
  `selected_decompiled.c`. `Get-DecompCoverageSnapshot.ps1` now reports client
  opcode coverage as `254` implemented, `72` partial, and `0` missing; the
  client missing-model queue is `None`. `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\missing-client-models-network\`
  succeeds with `0` warnings and `0` errors. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\missing-client-models-world\`
  succeeds with existing package-version warnings plus the existing
  `Spline.formation` warning.
  A later interior-wallpaper implementation gate passed:
  `dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-world-housing-interior-wallpaper\`
  and `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\world-server-housing-interior-wallpaper\`
  both succeeded with `0` warnings and `0` errors. Focused housing tests
  passed `59/59` for `HousingPacketShapeTests`,
  `ClientHousingNeighborHandlerTests`, `ResidenceTests`, and
  `PacketPlaceholderNamingTests`.

One-hundred-tenth AMP commit handler pass:

- Native evidence:
  focused `FindImmediateInstructions.java` on `0x01A2` found the packet
  registration row in `FUN_14006c290`, the sender at `1403d19a0`, and an
  unrelated result/dispatch branch. The registration row maps opcode `0x01A2`
  to advance helper `ClientCommitAmpSpec_AdvancePayload` (`140088270`) and
  writer `ClientCommitAmpSpec_WritePayload` (`140088290`). The advance helper
  adds `7` bits plus `count * 16` payload bits and `count * 2` semantic bytes.
  The writer emits a 7-bit count followed by a contiguous array of 16-bit AMP
  ids, matching the existing `ClientCommitAmpSpec` receive model.
- Sender mapping:
  `AbilityBook_SendClientCommitAmpSpec` (`1403d19a0`) reads the current spec AMP
  list, appends pending AMP ids, sends opcode `0x01A2` through
  `Network_SendOpcodePayloadHelper`, and clears the pending AMP cache. The
  packet carries no spec index, so NexusForever treats it as an active-spec
  request.
- NexusForever implementation:
  WorldServer now handles `ClientCommitAmpSpec` by validating the active spec,
  player alive/combat gates, AMP ids, and available AMP power before adding
  distinct new AMPs to the active action set. Validation failures return a
  `ServerActionSet` result using the existing `LimitedActionSetResult` AMP
  errors; successful commits send the refreshed `ServerAmpList`.
- Still blocked:
  exact client UI error presentation and deeper unlock/category-tier
  requirements remain bounded by the current table/runtime validators. This
  pass does not introduce a new asynchronous spell-update transaction gate.
- Coverage and verification:
  WildStar64 export-only Auto applies `602` source-controlled labels with `0`
  missing labels and creates the split advance helper function at `140088270`.
  The three new labels appear in `functions.csv`, `selected_reasons_summary.csv`,
  and `selected_decompiled.c`. `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -m:1 -v minimal --nologo` succeeds with `0` warnings and `0`
  errors. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\commit-amp-spec-world\`
  succeeds with only the existing `Spline.formation` warning.

One-hundred-eleventh instance-settings handler pass:

- Native evidence:
  the existing durable WildStar64 labels
  `Lua_GameLib_SetInstanceSettings` (`140701650`),
  `Lua_GameLib_ResetSingleInstance` (`1407017d0`), and
  `Lua_GameLib_OnClosedInstanceSettings` (`140701800`) map the solo instance
  settings dialog requests. `SetInstanceSettings` sends opcode `0x0163` with
  the selected instance portal id, validated difficulty, prime level, and
  packed setting bits. `ResetSingleInstance` sends opcode `0x0153` with the
  selected instance portal id. `OnClosedInstanceSettings` conditionally sends
  opcode `0x00D2` for the selected portal when the dialog closes.
- NexusForever implementation:
  WorldServer now handles `ClientClosedInstanceSettings`,
  `ClientResetSingleInstance`, and `ClientSetInstanceSettings` as conservative
  instance-setting requests. The close and settings-update paths are logged
  without mutating instance state. The reset-single-instance path returns
  `ServerInstanceResetResult` with `Success = false`, matching the existing
  failure-capable server packet instead of silently dropping the request.
  Invalid `WorldDifficulty` values above the named client range are rejected.
- Still blocked:
  actual solo instance reset and settings mutation remain blocked until
  NexusForever has a verified instance-lock/settings runtime model. The packed
  trailing setting bits beyond the known difficulty/prime/rally fields should
  stay diagnostic-only.
- Coverage and verification:
  `Get-DecompCoverageSnapshot.ps1` now reports client opcode coverage as
  `258` implemented, `68` partial, and `0` missing. The three instance-settings
  rows are marked `handled` in `opcode_coverage_inventory.csv`. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\instance-settings-world\`
  succeeds with only the existing `Spline.formation` warning.

One-hundred-twelfth housing plug update pass:

- Native evidence:
  `ClientHousingPlugUpdate_WritePayload` (`14009d560`) serialises opcode
  `0x0510` as residence identity, `HousingPlotInfo` id, `HousingPlugItem` id,
  32-bit plug facing, one zero/unknown 32-bit field, a 3-bit operation, and
  five 20-byte contribution records around
  `exports\WildStar64.exe\selected_decompiled.c:2776`.
  `Housing_SendClientPlugPlaceOrRotate` (`1404b73e0`),
  `Housing_SendClientPlugRemove` (`1404b7540`), and
  `Housing_SendClientPlugRepair` (`1404b7620`) build the shared request around
  `selected_decompiled.c:18875`, `18952`, and `19004`. The Lua wrappers
  `Lua_GameHousingPlot_PlacePlug`, `RemovePlug`, `RepairPlug`, and
  `SetPlugRotation` are selected around `selected_decompiled.c:44737`.
  `Housing_ResolvePlotInfoForSelectedResidence` (`1404bc060`) resolves the
  plot id from `HousingPlotInfo` by selected property and plot index around
  `selected_decompiled.c:19410`.
- NexusForever implementation:
  `ClientHousingPlugUpdate` now exposes the mapped fields instead of discarding
  them. WorldServer routes the request into `ResidenceMapInstance.PlugUpdate`,
  validates the residence, permissions, plot id, unknown zero field, plug table
  entry, plot type, and facing, and applies place/rotate or remove updates to
  the residence plot. `Plot.SetPlug` now validates plug table entries and
  default `HousingPlotInfo` plugs use that same path. Successful mutations
  refresh `ServerHousingPlots` and update the plug entity. Invalid
  plug/facing/inactive cases return
  `ServerHousingResult`; repair returns `Plug_ModifyFailed` because the current
  server has no verified plug damage/upkeep runtime.
- Still blocked:
  contribution cost charging, build timers, plug upkeep/repair state, and exact
  client success/failure timing remain open. The five contribution records are
  preserved on the model but stay diagnostic-only until a backing resource model
  is mapped.
- Coverage and verification:
  nine new WildStar64 labels were added to `function_labels.csv` and verified in
  `functions.csv` plus `selected_decompiled.c` after an export-only run with
  `-MaxDecompiledFunctions 650`. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\housing-plug-update-world\`
  succeeds with only the existing `Spline.formation` warning.

One-hundred-thirteenth vendor sell quantity pass:

- Native evidence:
  the GameLib registration table around `140c59b30` maps
  `SellItemToVendor` to `Lua_GameLib_SellItemToVendor` (`14050cd50`) and
  `BuybackItemFromVendor` to `Lua_GameLib_BuybackItemFromVendor`
  (`14050ce20`). The sell wrapper reads argument 1 as the item location,
  argument 2 as the quantity, and calls `Vendor_SendClientSellItemToVendor`
  (`1403c2630`). That helper validates the current vendor state and sends
  opcode `0x0166` with the item-location payload plus the requested quantity
  around `exports\WildStar64.exe\selected_decompiled.c:14362`. The buyback
  wrapper routes a selected buyback unique id through opcode `0x00BB` via
  `Vendor_SendClientBuybackItemFromVendor` (`1403c2850`) around
  `selected_decompiled.c:14464`.
- NexusForever implementation:
  `ClientVendorSellHandler` now honors the client quantity by rejecting zero or
  over-stack requests, computing rewards from the mapped
  `IItem.GetVendorSellCurrency`/`GetVendorSellAmount` helpers, and deleting
  exactly the requested count. `IInventory`/`Inventory` gained a
  quantity-aware `ItemDelete(ItemLocation, uint, ItemUpdateReason)` overload
  that returns the full removed item for whole-stack sells or creates a
  detached split item for partial-stack buyback while updating the original
  stack with the vendor reason. The already-implemented buyback inventory-space
  guard had its stale implementation marker removed.
- Still blocked:
  the wider buyback lifecycle still uses the existing in-memory buyback manager;
  durable persistence of expired or re-bought existing items is outside this
  pass. Exact client text for `ItemUpdateReason.Vendor` versus buyback display
  remains unverified, so the server keeps the existing vendor update reason.
- Coverage and verification:
  four new WildStar64 labels were added to `function_labels.csv` and verified in
  `functions.csv`, `selected_reasons_summary.csv`, and
  `selected_decompiled.c` after an export-only run with
  `-MaxDecompiledFunctions 650`. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\vendor-sell-world\`
  succeeds with only the existing `Spline.formation` warning. A focused marker
  scan of the vendor sell and buyback handlers now returns no matches.

One-hundred-fourteenth generic unlock item pass:

- Native and table evidence:
  opcode coverage already maps `ClientItemGenericUnlock` to opcode `0x0400`
  with an item-location payload. The client string table includes the generic
  unlock failure strings `InvalidGenericUnlock` and
  `GenericUnlock_AlreadyUnlocked`, and NexusForever already models
  `ServerGenericUnlockResult` (`0x0985`) as the 3-bit
  `GenericUnlockResult` enum. `Item2.GenericUnlockSetId` points at
  `GenericUnlockSet`, whose records fan out through up to six
  `GenericUnlockEntryId` fields; the account-item grant path already expands
  the same set shape before granting unlock entries.
- NexusForever implementation:
  `ClientItemGenericUnlockHandler` now resolves `GenericUnlockSet` instead of
  treating `Item2.GenericUnlockSetId` as a single `GenericUnlockEntry` id. It
  validates every nonzero entry id, returns `Invalid` for bad set data, returns
  `AlreadyUnlocked` without consuming the item when all entries are already
  known, consumes the item once when at least one entry is new, and grants only
  the still-locked entries.
- Still blocked:
  exact client presentation for partial multi-entry unlock sets remains bounded
  by the existing `GenericUnlockManager.Unlock` result behavior, which emits a
  result for each granted entry. No new character-scoped unlock behavior was
  inferred in this pass.
- Verification:
  `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\generic-unlock-world\`
  succeeds with only the existing `Spline.formation` warning. A focused marker
  scan of the generic unlock, vendor sell, and buyback handlers now returns no
  matches.

One-hundred-fifteenth activate/cast variant handler pass:

- Native evidence:
  existing durable WildStar64 labels map two remaining low-opcode request
  variants. `ActivateUnit0x0096_WritePayload` (`140093d30`) serialises opcode
  `0x0096` as the shared generated context token, two 4-bit selectors, target
  entity id, and world-position vector. `SpellCast_SendClient0x009dVariant`
  (`14039b340`) builds opcode `0x009D` with the shared generated context token,
  selected entry id, resolved target entity id, and target/fallback world
  position; `SpellCast_Dispatch0x009dSelectionMask` (`14039b930`) fans selected
  ids through that sender and reports local failures.
- NexusForever implementation:
  WorldServer now handles `ClientActivateUnitCastPosition` by routing the mapped
  target entity id and context token through the existing activate-unit cast
  path, while preserving selector and position values in trace diagnostics.
  `ClientCastSpellSelected` is handled as diagnostic-only: the payload is
  logged, but no spell is cast because the selected-entry id space and failure
  fan-out are still client-side runtime semantics rather than a verified server
  spell lookup.
- Still blocked:
  the `0x0096` selector nibbles and position are not yet proven to affect server
  activation spell choice, so they remain diagnostic. `0x009D` mutation remains
  blocked until the selected-entry id can be correlated with a server-owned spell
  or cast-entry table and the local `SpellCastFailed` fan-out behavior is
  mapped.
- Coverage and verification:
  `Get-DecompCoverageSnapshot.ps1` marks `ClientActivateUnitCastPosition` and
  `ClientCastSpellSelected` as handled. In the current dirty workspace, the
  snapshot reports client opcode coverage as `268` implemented, `58` partial,
  and `0` missing because it also observes other pending handler files already
  present in the tree. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\activate-cast-variants-world\`
  succeeds with only the existing `Spline.formation` warning.

One-hundred-sixteenth class innate stance pass:

- Native and table evidence:
  the GameLib method table around `140b72850` maps the class innate accessors
  `GetClassInnateAbilitySpells`, `GetNumClassInnateAbilitySpells`,
  `GetCurrentClassInnateAbilitySpell`,
  `GetCurrentClassInnateAbilityIndex`,
  `SetCurrentClassInnateAbilityIndex`, and
  `IsCurrentInnateAbilityActive` to the `1406f69e0`-`1406f7080` function
  cluster. The selected export now includes those labels around
  `exports\WildStar64.exe\selected_decompiled.c:45853`. The NexusForever
  `Class` table model exposes the matching client-facing innate slots as
  three `Spell4IdInnateAbilityActive` entries, while
  `ServerStanceChanged` serialises the selected index in two bits.
- NexusForever implementation:
  `ClientSetStanceHandler` now resolves the player's `Class` table row and
  rejects stance indices outside the active innate slot array or pointing at an
  empty active innate spell id before updating `Player.InnateIndex` and echoing
  `ServerStanceChanged`. This closes the stale validation marker without adding
  new prerequisite or spell-cast side effects. The stale item-use prerequisite
  marker beside `GenericError.UnlockItemFailed` was also removed because the
  client string table already exposes the matching `UnlockItemFailed` text and
  the server behavior was already using that error.
- Still blocked:
  prerequisite evaluation for class innate unlocks remains bounded by the
  existing character/class setup. This pass validates the selected table slot,
  but does not infer new client behavior for locked innate variants.
- Coverage and verification:
  six new WildStar64 labels were added to `function_labels.csv` and verified in
  `functions.csv`, `selected_reasons_summary.csv`, and
  `selected_decompiled.c` after an export-only run with
  `-MaxDecompiledFunctions 650`. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\set-stance-world\`
  succeeds with only the existing `Spline.formation` warning. A focused marker
  scan of `ClientSetStanceHandler` now returns no matches.

One-hundred-seventeenth mass handler coverage pass:

- Work done:
  57 missing client-opcode handlers were created across two waves (Wave 1: 24 new
  files + 1 fixed broken interface, Wave 2: 25 new files, Wave 3: 8 new files)
  bringing `clientHandled` from ~268 to 318, and reducing `clientModelOnly` from
  33 to 8 to 0.
- Wave 1 handlers (24 new + 1 fix):
  Path (Explorer/Scientist), Vehicle (Embark), Fortune (FlipCard, NotifyStorefront,
  Start), Misc (ChallengeChoice, GetRealmTransferDestinations), Guild (BankTransaction
  x2, BankTabOpen, BankMoneyTransaction, PerkActivate, BankTabRename, StandardModify,
  SetStandard), Housing (VisitResidence), Item (ItemContextAction), GalacticArchive
  (Unlock, Viewed). Fixed `ClientHousingRenamePropertyHandler` - added missing
  `IMessageHandler<>` interface declaration.
- Wave 2 handlers (25 new):
  Instance (LeavePendingRemoval, RaidInfoRequest), Info (RealmInfoRequest),
  Matching (QueueLeaveAsGroup, InitiateVoteToKick, InitiateVoteToSurrender,
  InitiateLookingForReplacements, QueueRandomParty, StopLookingForReplacements,
  CastVoteKick, CastVoteSurrender), Misc (PlayerMovementSpeedUpdate), Path
  (MissionAttemptScientistExperimentation, SettlerImprovementBuildTier,
  SoldierImprovementBuild), Pet (PetSetStance), Misc (PtrCopy,
  PublicEventRequestScoreboard, CinematicCameraSubjectPosVel,
  CinematicCameraSubjectSpline, CinematicCameraSubjectUnit, CinematicWhiteOutFinished),
  Guild (RecruitmentGuildGetDetailedGuildInfo, RecruitmentGuildSubscribe),
  Character (CharacterRename). Fixed `ClientMatchingQueueRandomParty` model -
  added missing `: IReadable` to the class declaration.
- Wave 3 handlers (8 new):
  Fortune (NotifyGame), Instance (ResetInstances), Misc (Spline2Request,
  SteamAchievements), Spell (CastGuildBossToken), Guild (WarPartyBossTokensRequest),
  Leaderboard (PvpRequest, PveRequest - new handler folder created).
- Ghidra probe results:
  `DumpNearbyData 1406febc0` - all slots show `<no defined data>` (dead end).
  `TraceStringReferences "AMPSave"` - 0 cross-references (dead end).
  0x00FB payload confirmed as single float from movement struct offset `+0x12`
  (matches `ClientMovementFallDamage.FallStateValue`).
  0x00FC payload confirmed as position struct from offsets `+0x40..+0x48`
  (matches landing state model).
- Coverage and verification:
  `LATEST_COVERAGE_SUMMARY.json` after all three waves: `clientHandled=318`,
  `clientModelOnly=0`, `missingClientHandlers=0`. Label count unchanged at 621.
  `dotnet build NexusForever.WorldServer --no-incremental -v quiet` succeeds with
  only the existing `Spline.formation` warning.

One-hundred-eighteenth final opcode coverage closure pass:

- Native registration evidence:
  `FindImmediateInstructions` and `InspectCodeAddress` closed the remaining
  opcode-coverage queues. `0x0635`, `0x081A`, `0x081B`, and `0x081C` register in
  `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`), the same client-send
  table as `ClientSpline2Request` `0x081D`; they are now modeled as
  `ClientMovementControlAck`, `ClientSpline2NodeData`,
  `ClientSpline2NodePositionData`, and `ClientSpline2DataRequest` with
  diagnostic handlers. Server-side receive models were added from the
  WildStar64 reader helpers: `0x0145` reads a 3-bit appearance result at
  `14007fdc0`, `0x0188` reads a counted raw-uint flight-path list through
  `ServerFlightPathUpdate_ReadPayload` (`14008eaa0`), `0x019E` uses the shared
  one-uint helper, and `0x0551` reads a counted nested spell list through
  `ServerSpellList_ReadPayload` (`140096060`) plus the `140094890`/`140094aa0`/
  `140094bf0` entry helpers. `0x089B` was confirmed by
  `ServerVehiclePassengerSelf_ReadPayload` (`1400979e0`) as self id, vehicle id,
  2-bit seat type, and 3-bit seat position.
- NexusForever implementation:
  the opcode enum was renamed away from the last numeric placeholders; structural
  models were added for `ServerCharacterAppearanceResult`,
  `ServerFlightPathUpdate`, `ServerAttributePoints`, and `ServerSpellList`;
  `Server0237` became `ServerUiWindowState`; `Server089B` became
  `ServerVehiclePassengerSelf`; and the spell-broadcast family now has named
  enum, class, and file names while retaining the conservative structural model
  fields. `Server_0x178_SoundTrigger` was also renamed to `ServerSoundTrigger`
  so the model tree no longer carries mapped server models under numeric names.
  Vehicle passenger setup now sends `ServerUiWindowState` and
  `ServerVehiclePassengerSelf`. The two core enum shells `State` and `State2`
  now have inert empty models so the coverage inventory no longer reports them
  as enum-only rows.
- Still blocked:
  several field names inside the spell-list and spell-broadcast nested
  structures remain structural labels because the client parse proves width and
  ordering but not the gameplay meaning of every member. No runtime behavior was
  broadened from those structural names.
- Coverage and verification:
  `Get-DecompCoverageSnapshot.ps1` now reports `Client 330/330`, `Server
  566/566`, `Core 3/3`, and all four queues as `None` in
  `Decomp\Analysis\coverage\LATEST_COVERAGE_SUMMARY.md`. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -v minimal --nologo` succeeds with `0 Warning(s)` and `0 Error(s)`.

One-hundred-nineteenth item binding follow-up:

- Native client evidence:
  `InspectCodeAddress 1403c17d0` maps the local item move validator. Before the
  client sends opcode `0x0182 ClientItemMove`, it checks an item-template byte at
  offset `+0x150` for bit `0x10`; if that bit is set and the item is moving into
  the equip path, the client dispatches the named event
  `ItemConfirmSoulboundOnEquip`. `DumpNearbyData 140b2f3c0` shows the matching
  Lua item bind payload keys around `tBind`, `bSoulbound`, `bNoTrade`,
  `bOnPickup`, and `bOnEquip`.
- NexusForever implementation:
  item instances now persist a `soulbound` state, expose `IItem.MakeSoulbound`,
  preserve that state on stack splits and partial deletes, bind items when
  moved into equipped slots if `Item2.BindFlags` contains `0x10`, and bind
  costume-unlocked items after a successful account costume unlock. Soulbound
  items are rejected from player trades and outgoing mail attachment sends.
  The `1403c17d0` function label was added as
  `ItemMove_ValidateAndMaybeConfirmBindOnEquip`.

One-hundred-twentieth realm-first achievement follow-up:

- Native client evidence:
  `InspectCodeAddress 1403f38b0` maps the client-side realm-first presentation
  path. The handler resolves the completed achievement, builds the achievement
  name payload, and dispatches `RealmFirstAchievementAnnounce` with the
  achievement id, guild/player flag, announcing name, and payload. The client
  data mapping identifies realm-first achievement rows as achievement type
  `116`.
- NexusForever implementation:
  `AchievementType.RealmFirst` now names type `116`. `GlobalAchievementManager`
  loads already-completed character and guild realm-first ids from the character
  database, claims a realm-first id only once per process, and the character and
  guild achievement managers broadcast `ServerRealmFirstAchievement` when a
  realm-first achievement is completed for the first time on that realm. The
  residence autosave interval cleanup was also made real by moving the hardcoded
  housing save period behind `WorldConfig.ResidenceSaveIntervalSeconds`.
  Command categories now instantiate through `ActivatorUtilities`, and the
  broadcast/realm command handlers use constructor-injected world services
  instead of resolving those dependencies at command execution time.

One-hundred-twenty-first marker completion follow-up:

- Old-branch/script evidence:
  the `kirmmin/latest` map scripts had concrete zone/objective behavior for
  Crimson Isle crash-site quest `5596` objective `8255`, and Northern Wilds
  Empowered Tower quest `3486` objective `4987` plus story panel `1575`.
  Current `WorldEntity` only invoked entity `OnEnterZone` hooks, so the current
  map-script markers could not run before a map-level zone hook existed.
- NexusForever implementation:
  map scripts now receive `OnEnterZone` through `IBaseMap`/`BaseMap`; Crimson
  Isle and Northern Wilds port the old objective/story-panel callbacks into the
  current script system; spline AI now supports negative spline speeds by
  flipping to the paired reverse mode; Slaughterdome match finish and
  resurrection flows use `TeleportToLocal` with team map entrances; and public
  events now receive death/resurrection callbacks so the Slaughterdome event can
  track deaths, auto-release to Holocrypt, and move resurrected players back to
  their team spawn.
- Server/service markers resolved:
  STS error XML now writes real `server`, `module`, `line`, and `text`
  attributes; friendship email account lookup hydrates missing accounts through
  the account API; internal outbox payload serialization is shared by chat,
  friendship, and group servers; guild disbands now publish guild type and the
  chat server removes guild-referenced channels; group member absorption and
  healing-absorb values now flow from world to group server; and auth initial
  realm selection now prefers an online realm that already has account inventory
  targeted to it before falling back to the first online realm.
- Verification:
  at that checkpoint, the literal marker scan for `Source` and this findings
  file returned no active TODO-style source items, and
  `dotnet build Source\NexusForever.sln --no-restore -m:1 -v minimal
  --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\todo-full-pass\`
  succeeds with `0 Warning(s)` and `0 Error(s)`.

One-hundred-twenty-second blocked-marker implementation follow-up:

- Inventory expiration and rollback:
  `Item2Entry.ExpirationTimeMinutes` and the existing item database/network
  `ExpirationTimeLeft` field are now wired end-to-end. Items load persisted
  expiration, new item instances initialize finite lifetimes in seconds, player
  update ticks inventory expiration, expired items delete with
  `ItemUpdateReason.Expired`, and network item payloads include the current
  time left. Stack creation, stack moves, and splits preserve expiration
  boundaries so finite-life stacks are not merged with different remaining
  timers. `IBag`/`Bag` now expose positional snapshots, and `Inventory.ItemMove`
  preflights shrinking equipped bag capacity and restores bag/item state on
  unexpected multi-step move failures.
- Repair vendor:
  focused WildStar64 inspection mapped `Item_CalculateMaxDurability`
  (`1403b5400`), `Item_CalculateDurabilityValue` (`1403b5360`),
  `Item_CalculateRepairCost` (`1403b54a0`), and
  `ServerItemDurabilityUpdate_ReadPayload` (`1403b9690`). The repair cost
  helper uses GameFormula `0x022F`, the missing durability fraction, and an
  item value term. NexusForever now validates a selected vendor, calculates
  repair costs server-side, rejects insufficient credits, charges credits, and
  restores durability for single-item and repair-all requests. Repair-all
  client totals are logged on mismatch but not trusted.
- Flight-path purchase:
  `ClientFlightPathPurchaseHandler` now resolves the validated contiguous
  route chain to the destination `TaxiNode.WorldLocation2Id`, checks teleport
  eligibility, charges the summed route price, and teleports to the destination
  world location. This completes paid route travel with current server movement
  primitives; cinematic taxi spline/vehicle embark visuals remain a separate
  unmapped precision gap.
- Verification:
  WildStar64 export applied `638` labels with `0` skipped or missing, and the
  four new repair labels appear in `functions.csv`, `selected_reasons_summary.csv`,
  and `selected_decompiled.c`. Full solution builds succeeded with isolated
  output paths for inventory, repair, and flight-path slices:
  `.nexusforever-runtime\build\blocked-inventory\`,
  `.nexusforever-runtime\build\blocked-repair\`, and
  `.nexusforever-runtime\build\blocked-flight\`, each with `0` warnings and
  `0` errors. After the final inventory resize safety adjustment, the full
  solution build was rerun at
  `.nexusforever-runtime\build\blocked-final-2\` and also completed with
  `0` warnings and `0` errors.

Current broad-marker implementation audit:

- Implemented in the follow-up pass:
  account pending-item groups now have real runtime state, pending-list packets,
  claim/return handling, and online character/account gift routing; storefront
  account gifts deliver pending groups to online recipients instead of rejecting
  every non-self target. Tradeskill learn, talent pick, and talent reset now
  mutate character profession state, send profession load/update/cooldown
  packets, and persist through the new `character_tradeskill` table. Marketplace
  item auctions and commodity orders now use an in-process order book with post,
  search, bid, buyout, cancel, and owned-list behavior. CREDD exchange requests
  now use an in-process buy/sell/cancel lifecycle with credit escrow for buy
  orders and completion operation results. Complex/catalyst craft requests now
  reuse fixed-recipe material validation and consume extra catalyst/power-core
  items when present; additive/abandon requests update runtime crafting modifier
  state. Rune slot add/clear/install/reroll now keep runtime rune-slot state,
  consume/recover rune items, and return mapped sigil results.
- Spell/runtime follow-up:
  broad `evidence-gap-*` diagnostic labels were removed from source. The
  spell runtime now applies nonzero spell-immunity modes through the existing
  immunity state, allows mapped housing teleport behavior across mode values,
  resolves clamp-vital target vitals from the interpreted payload, clamps
  generic vitals through `UnitEntity`, and handles positive integer spell
  cooldown extension.
- Live unmapped-payload completion:
  storefront offer item payloads now preserve and deliver the nonzero
  account-item branches instead of rejecting `IOfferItemData.Type != 0`;
  fixed-recipe crafting now generates and delivers `LootId` outputs through
  `GlobalLootManager`; spell vital, sap-vital, cooldown, personal damage/heal,
  proc target, proc trigger, unit-state, busy-state, shield-overload, path XP,
  level-scaled XP, and threat mode payloads now have runtime behavior instead
  of unmapped/evidence-gap skips. The one live `ThreatModification` mode `130`
  row on spell `32563` is implemented as the table's spell-level damage threat
  multiplier (`ThreatMultiplier=0.1`) and is folded into generated damage
  threat.
- Current broad marker scan interpretation:
  `rg -n "evidence-gap|service-unavailable|store-unavailable|unsupported|not implemented|TODO"
  Source\NexusForever.WorldServer Source\NexusForever.Game
  Source\NexusForever.Game.Abstract Source\NexusForever.Network.World` returns
  no source hits after this pass. Historical notes in this file still mention
  earlier unsupported/evidence-gap states for audit continuity.
- Verification:
  `rg -n "evidence gap|evidence-gap|loot-output-not-modeled|unmapped-|unknown-mode|secondary-payload|parameter-driven|ambiguous-payload|ambiguous-datafloat01|percent-out-of-range"
  Source\NexusForever.Game Source\NexusForever.WorldServer
  Source\NexusForever.Network.World Source\NexusForever.Game.Abstract` returns
  no hits after the live-payload pass. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\unmapped-payload-fixes-final-2\`
  succeeds with `0 Warning(s)` and `0 Error(s)`.

Offline WildStar wiki path ability follow-up:

- Settler campfire evidence:
  the archived `Settler's Campfire` wiki page says the activated campfire grants
  Back in Action for 60 minutes with max-health bonuses of 3%, 4%, and 5% by
  tier. Client `Spell4Effects` rows for spells `32759`, `32771`, and `32772`
  contain the direct `SettlerCampfire` effect (`0x0069`) with
  `DataBits00=0/1/2` and a 3600000 ms duration. The matching Back in Action
  activation spells `32766`, `32777`, and `32778` apply
  `UnitPropertyModifier` to `Property.BaseHealth` with multipliers `1.03`,
  `1.04`, and `1.05` for the same duration.
- Runtime scope:
  NexusForever now maps the direct `SettlerCampfire` effect tier index to the
  corresponding Back in Action spell and casts it through the existing spell
  pipeline. This is intentionally limited to the table-backed buff bridge; it
  does not claim to implement spawned campfire object behavior, aura seating
  rules, or rested-XP/decor mechanics.
- Remaining blocker:
  one-hop proxied campfire aura spells `32760`, `32775`, and `32776` expose
  `RestedXpDecorBonus` (`0x006D`) in addition to handled heal/fluff/despawn
  effects. The live client table also uses that effect for housing decor
  comfort/lighting/aroma/ambience/pride spells with `DataBits00` interpreted as
  floats such as `0.2`, `0.4`, and `0.6`; campfire tiers use `0.25` and `0.5`
  plus `parameterValue00=0.04`. NexusForever currently persists `RestBonusXp`
  and accrues housing rest XP on login, but there is no mapped evidence yet for
  how this spell effect modifies the persisted pool over time, how
  `parameterValue00` participates, or how decor bonuses stack. The effect
  therefore remains diagnostic/audit-only until its payload and runtime
  persistence behavior are mapped from stronger client/server evidence.
- Rested XP direct modifier follow-up:
  `ModifyRestedXP` (`0x0086`) is separate from the decor/aura bonus. The live
  client table has only three rows for it: `71362` (`Primal Elixir- Bonus Rest
  XP - Mystery Box Item`) with `DataBits00=1.5f`, `81790` debug with
  `DataBits00=-1.4999f`, and `82271` (`MTX Store - Restorative Flask - Fills
  Rest XP Bar`) with `DataBits00=5.0f`. That supports interpreting
  `DataBits00` as a signed current-level XP span multiplier, clamped by the
  same rested XP cap the server already uses. NexusForever now implements this
  direct spell effect by modifying persisted `RestBonusXp`; it intentionally
  does not treat `RestedXpDecorBonus` as equivalent.
- Client rested-XP UI accessors:
  `TraceStringReferences` found no direct code xrefs for the
  `RestedXpDecorBonus` or `ModifyRestedXP` enum-name strings. The actual Lua
  accessors are registered in a callback table: `GameLib.GetRestXp` starts at
  `14050cbc0` and returns the current player state field at offset `0x1688`,
  while `GameLib.GetRestXpKillCreaturePool` at `14050cbf0` reads the same field
  and applies a coefficient from a client lookup (`FUN_140200220(0x155)`,
  value at `+0x18`) for display. These are mapped as read/display accessors,
  not evidence for decor-bonus persistence or stacking.
- Rested XP state mutation decompile pass:
  a focused `0x1688` sweep plus caller/string tracing maps the client-side
  rested-XP pool as current-player state rather than a spell-effect runtime
  accumulator. `ServerPlayerCreate_ApplyPlayerXpState` at `1403b5f80` copies
  total XP from payload offset `0x90` into player offset `0x1678`, copies the
  rested XP pool from payload offset `0x94` into offset `0x1688`, then calls
  `PlayerXpState_DispatchUiXpChanged` and later dispatches `CharacterCreated`
  from string `140afb680`. The same UI helper at `1403c6d80` reads `0x1688`
  and dispatches `UI_XPChanged` from string `140afd380` with the rested pool as
  the last visible argument. `ServerExperienceGained_ApplyPlayerXpState` at
  `1403c8760` adds the packet's total-XP field to `0x1678`, subtracts the
  packet's rested-XP amount from `0x1688`, calls the same UI helper, and
  dispatches `ExperienceGained` from string `140afd4b8` including the rested
  branch with reason `0x10`. This correlates directly with NexusForever
  `ServerPlayerCreate.RestBonusXp` and `ServerExperienceGained.RestXpAmount`.
  Additional current-player/state-machine handlers at `140037f30` and
  `1400461e0` also copy state payload values into `DAT_140c635f0 + 0x1688`,
  but the pass found no client function that adds decor/aura value into the
  rested pool over time.
- RestedXpDecorBonus blocker state:
  client `Spell4Effects` rows for effect `109` are table evidence only. The
  campfire/decor rows use `DataBits00` float magnitudes from `0.1` through
  `2.0`; campfire and sleeping-bag-style rows also carry
  `parameterValue00=0.04` and a not-in-combat prerequisite (`390`). The native
  client has no code reference to the `RestedXpDecorBonus` enum-name string at
  `140abc1b0`; `TraceStringReferences` reports zero refs, and the rested-XP
  field sweep found only state copy, UI read, Lua read, constructor/reset, and
  experience-consumption paths. A follow-up `TraceStringReferences.java` pass
  on `RestedReward` at `1409d0be0` found only a data pointer at `140c1b630`
  and no function xref chain, so it does not identify an accrual or stacking
  path either. This leaves the effect mapped-only/blocked:
  server implementation still needs stronger evidence for accrual timing,
  whether `DataBits00` is additive or multiplicative, how
  `parameterValue00=0.04` participates, stack/cap rules across decor sources,
  and persistence timing.

Offline wiki quest/tradeskill/Galactic Archive implementation follow-up:

- Quest reward enum evidence:
  focused `InspectCodeAddress.java` at WildStar64.exe address `1406625d0`
  maps the Lua `Game.Quest` reward constants. The durable label
  `Lua_RegisterQuestConstants` records `Quest2RewardType_Item=1`,
  `Reputation=2`, `Money=3`, `TradeSkillXp=4`, `GrantTradeskill=5`,
  `AccountItem=6`, `AccountCurrency=7`, `GenericUnlockAccount=8`,
  `GenericUnlockCharacter=9`, and `RotationEssence=10`. Client
  `Quest2Reward.tbl` rows with type `4` use `objectId` values from
  `TradeskillTier.ID`, which map to `TradeskillTier.TradeSkillId` and tier XP
  fields; type `5` rows use profession ids; type `7` rows use account currency
  ids. NexusForever now grants the mapped tradeskill XP, profession, and
  account-currency quest rewards through `QuestManager`.
- Quest prerequisite scope:
  NexusForever now validates `Quest2.PrerequisiteItem` through the runtime
  inventory count API and blocks `QuestIdExclusionPreq0..2` when the excluded
  quest is already known in the character's active, inactive, or completed
  quest state. `QuestPrerequisite_CanAccept` (`140552550`) delegates faction
  reputation gates to `QuestPrerequisite_CheckFactionLevel` (`140552eb0`),
  which walks the three `Quest2.FactionIdPreq*` fields at offsets `0xb0`,
  `0xb4`, and `0xb8`; for each nonzero faction it compares the current faction
  level against `FactionLevelPreq*` at `+0x0c`, using `FactionLevelCompPreq*`
  at `+0x18` as the comparator. A clear comparator bit requires current level
  `>=` required level, and a set comparator bit requires current level `<=`
  required level. NexusForever now applies that logic through
  `QuestManager.MeetsFactionLevelRequirement`.
- Quest receiver-routing scope:
  `Dialog_BuildCreatureQuestResponses` (`140557470`) builds creature dialog
  quest responses by walking the selected creature's `Creature2.QuestIdGiven`
  and `Creature2.QuestIdReceive` arrays and local quest runtime state;
  `Dialog_BuildCommunicatorQuestResponses` (`140557e50`) does the same for
  communicator-delivered quests. Both paths feed `Dialog_SendClientQuestComplete`
  (`140557000`), whose `ClientQuestComplete` packet remains only
  `{ quest id, reward selection, communicator bit }`. Client data also
  correlates the `Quest2` alternate receiver prerequisite/direction rows with
  ordinary `Creature2.QuestIdReceive` receivers: the pass found 207 Quest2 rows
  with alternate receiver prerequisite/direction data and all 207 have at least
  one Creature2 receiver. This maps `Quest2.WorldLocation2IdReceiver`,
  `WorldLocation2IdAltReceiver*`, `PrerequisiteIdAltReceiver*`,
  `QuestDirectionIdAltReceiver*`, and `QuestDirectionIdCompletion` as
  client-side dialog/map guidance metadata rather than server completion
  authority. NexusForever should continue validating non-communicator
  completion through visible `Creature2.QuestIdReceive` receivers unless a
  future packet/state producer exposes an authoritative receiver/location
  selection.
- Quest virtual item follow-up:
  `Lua_GameItemData_GetVirtualItems` (`1404165e0`) registers the
  `Game.ItemData.GetVirtualItems` Lua method. Its quest branch calls
  `QuestRuntime_BuildVirtualItemList` (`1405fcd70`), which walks current quest
  runtime state, reads the four `Quest2.VirtualItemIdPushed*` ids at offsets
  `0x160..0x16c`, counts at `0x170..0x17c`, and objective flags at
  `0x180..0x18c`, and returns virtual item triples with a quest owner id. It
  also derives virtual items from `QuestObjective` type `32`
  (`VirtualCollect`) when objective state and flags allow it. This maps pushed
  quest virtual items as a client-derived UI/inventory projection from local
  `Quest2` data plus server-sent quest/objective state; no separate server
  item grant or persistent inventory mutation is supported by this evidence.
  The existing server `VirtualCollect` objective updates from virtual loot stay
  the runtime mutation path.
- Schematic/tradeskill runtime scope:
  learned and discovered `TradeskillSchematic2` state is now stored per
  character, emitted in the profession load packet, and updated by the
  `GiveSchematic` spell effect path. Fixed-recipe crafting now grants tier
  craft XP from `TradeskillTier`, and quest reward type `4` uses the same
  tier-to-profession mapping. The additive packet path is implemented only up
  to the mapped safe boundary: `ClientCraftingAdditive_WritePayload`
  (`1400a5f20`) serializes the crafting station plus additive and catalyst
  `Item2` ids, and `Crafting_SendClientCraftingAdditive` (`14059b7c0`) performs
  client-side max-additive/error gating before sending opcode `0x084A`.
  NexusForever validates those `Item2` rows through `TradeskillAdditiveId` and
  `TradeskillCatalystId`, bounds transient modifiers by `MaxAdditives`, consumes
  the selected additive/catalyst items with `TradeskillAdditiveCost` after a
  successful fixed-recipe craft, and clears the transient modifier list. Hot/cold
  discovery math, fail/crit outputs, additive/catalyst output math, harvesting
  behavior, and durable rune state remain blocked pending narrower
  packet/table/runtime evidence.
- Achievement trigger scope:
  client achievement rows and DataMapping names now correlate several
  additional type ids with server-owned events: world-zone entry (`8`), crafted
  item (`35`), tradeskill tier (`37`), crafted-item checklist (`40`),
  reputation level (`42`), character level (`54`), title earned (`56`), path
  level (`64`), earned currency (`75`), guild beloved-reputation totals (`67`),
  guild max-path-level totals (`92`), duel participation (`97`), duel wins
  (`98`), class level-50 transitions (`103`), critical deathblows (`105`),
  guild/circle joins (`106`), group joins (`107`), friend additions (`108`),
  housing plug placement (`111`), housing decor purchases (`113`), contract
  quality completions (`130`), contract type completions (`131`), overall
  contract completions (`133`), account currency collection (`137`), costume
  unlock ownership (`38`), costume set ownership (`141`), and total primal
  essence collection (`152`). The
  world-zone entry rows are data-backed by
  ids `1386`/`2414`/`2637`-`2641`, all of which carry the target
  `WorldZoneId` with no object ids; NexusForever filters these type `8` rows
  against the actual `Player.OnZoneUpdate` zone id before advancing them. The
  guild beloved-reputation rows are
  data-backed by `Achievement.tbl.sql` ids `4119`/`4120`/`4122`, whose
  `ObjectId` is `10` (`FactionLevel.Beloved`) and whose values are cumulative
  guild thresholds `75`/`150`/`250`; NexusForever updates them only when a
  character crosses into `Beloved` in server-owned reputation state, forwarding
  through the existing guild achievement manager. The class level-50 rows are
  data-backed by ids `4941`-`4963` and `5092`/`5115`-`5119`, whose `ObjectId`
  values match the playable `Class` enum ids `1`/`2`/`3`/`4`/`5`/`7`; the server
  updates them when a character reaches the data-backed level-50 cap, which
  covers both personal and guild rows through the normal character-to-guild
  achievement forwarding path. The guild max-path-level rows are data-backed by
  ids `4615`-`4617`, all guild-only rows with values `10`/`20`/`35`; the server
  updates them when a character reaches `PathRewardGrant.MaxPathLevel` through
  the persisted path XP/level reward path. The critical-deathblow rows are
  data-backed by ids `4929`-`4938`, all with zero object filters and cumulative
  values; NexusForever advances them from the damage pipeline only when the
  authoritative `IDamageDescription` says the hit both killed the target and
  had `CombatResult.Critical`. The earned-currency rows are
  data-backed by `Achievement.tbl.sql` type `75`
  object ids matching `CurrencyType.Credits = 1` and `CurrencyType.Renown = 2`,
  and NexusForever updates them from actual credited `CurrencyManager`
  additions after caps are applied. The group and friend rows are data-backed by
  ids `4885`/`4886` and update from the internal group-join and friendship-add
  or friend-and-rival update handlers. Housing plug/decor rows are data-backed
  by ids `4996`-`4998`/`4999`-`5002`; NexusForever updates them from successful
  non-rotation plug placement and costed decor purchase paths only. The
  contract rows are data-backed by `Achievement.tbl.sql` ids `6063`/`6064` and
  `6067`-`6075`: type `133` uses `ObjectId=13`, which matches the `Quest2.Type`
  used by contract quests; type `130` uses `ObjectId` values from
  `PeriodicQuestGroup.ContractQualityEnum`; and type `131` uses `ObjectId`
  values from `PeriodicQuestGroup.ContractTypeEnum`. NexusForever updates those
  rows only after a server-owned contract quest completion, and only emits the
  type/quality counters when the completed contract has a valid
  `PeriodicQuestGroup` row. Type `129` (`On the Right Track`) stays blocked
  because its `ObjectId=4` does not map to the persisted contract quest/type/
  quality state and appears to need a separate contract-track or streak source.
  Account currency collection rows are data-backed by account-currency object ids in
  `Achievement.tbl.sql` (for example OmniBits `6` and essence ids `15`-`18`);
  NexusForever updates them from quest, loot, and account-item claim
  account-currency grants where the active player context is available. The
  type `152` Primal Matrix rows `7130`/`7143`/`7144` have `ObjectId=0` and
  thresholds `375000`/`1250000`/`4000000`; DataMapping names/descriptions say
  they count collected primal essences, and `AccountCurrencyType.tbl.sql` maps
  primal essence currencies to `Crimson=15`, `Cobalt=16`, `Viridian=17`, and
  `Violet=18`, so the same server-owned account-currency grant helper now also
  advances type `152` only when one of those four currency ids is credited.
  Catalog/store purchase state remains outside this mapped trigger path until a
  grant is claimed or otherwise server-owned. The duel rows are data-backed by
  `Achievement.tbl.sql` ids `4372`-`4374`
  (participate) and `4251`/`4370`/`4371` (wins), plus matching Jabbithole
  names/descriptions; NexusForever updates them from the server-owned active
  duel finish path for defeated/client-forfeit results. The guild/circle join
  rows are data-backed by `Achievement.tbl.sql` ids `4883`/`4884`, whose
  `ObjectId` values match `GuildType.Guild = 1` and `GuildType.Circle = 2`;
  NexusForever updates them from `GuildManager.JoinGuild`. These are
  implemented through existing runtime events rather than native enum-name
  decompile evidence. Costume ownership rows are data-backed by
  `AchievementChecklist` item ids: type `38` rows such as `4749`/`4752`/`5339`
  use single `Item2` ids for wardrobe-unlock items, while type `141` rows such
  as `6825`/`6829`/`6830` and `6942`/`6943` use checklist `Item2` ids for
  multi-piece costume sets. NexusForever now advances both type ids only after
  a successful server-owned `AccountCostumeUnlock` mutation from
  `ClientCostumeItemUnlock`, which is persisted in the auth account model and
  echoed through the existing costume item list/unlock packets. Type `140`
  remains blocked: its `Protostar Premium Fabkit Owner` checklist ids
  `535`-`538` are `HousingPlugItem` ids, and the current housing plug placement
  path explicitly rejects non-default, costed, prerequisite, upkeep, and upsell
  plugs rather than owning the premium fabkit unlock/purchase state. Type `45`
  is now implemented as counted quest-completion checklist achievements rather
  than broad zone quest counters: DataMapping has 23 type `45` rows and 315
  checklist rows, 311 of those checklist `ObjectId` values resolve to
  `Quest2.ID`, and every type `45` achievement has at least `Value` Quest2-backed
  checklist rows available. The remaining four Whitevale checklist object ids
  (`5415`, `5563`, `5565`, and `5566`) do not resolve to `Quest2.ID`; they stay
  inert in the quest-completion trigger, which is safe because the corresponding
  achievements need only 20 completions from larger 29-row lists. The client
  progress helper maps type `45` to a raw `Data0` value family rather than the
  checklist bit-count family (`Achievement_ProgressCategoryFromType` at
  `1406428d0`, consumed by `Achievement_GetProgressValue` at `140642b30`), so
  NexusForever stores the visible counted progress in `Data0` and uses `Data1`
  only as a server-side credited-checklist bitmask to prevent duplicate quest
  credit. Type `5` is now
  implemented as Creature2 kill checklist achievements: all 428 unique type `5`
  checklist object ids in the mapped client achievement rows resolve to
  `Creature2.ID` values in `Creature2.tbl.sql`, and the client progress helper
  maps type `5` to the checklist bit-count family
  (`Achievement_ProgressCategoryFromType` at `1406428d0`, consumed by
  `Achievement_GetProgressValue` at `140642b30`). NexusForever now advances
  type `5` from the same server-owned creature kill reward path as type `1`,
  while guild type `5` rows are forwarded only when the killing player is in an
  online group with more than one member and every member belongs to the same
  guild. This keeps the "all guild group" rows from advancing for solo or mixed
  groups. Type `6` is now implemented as Quest2 completion checklist
  achievements: DataMapping has 436 type `6` rows with 1,741 checklist rows,
  every nonzero checklist `ObjectId` resolves to `Quest2.ID`, and the row
  descriptions cover episode completion, quest-delivery bundles, and other
  multi-quest achievements. The client progress helper maps type `6` to the
  same checklist bit-count family as type `5` and type `7`
  (`Achievement_ProgressCategoryFromType` at `1406428d0`, consumed by
  `Achievement_GetProgressValue` at `140642b30`). NexusForever now advances
  type `6` from the existing server-owned quest completion path immediately
  after the scalar quest-complete trigger, using the completed `Quest2.Id` as
  the checklist object id. Type `7` is now
  implemented as completion-driven meta achievements: `AchievementChecklist`
  rows for type `7` overwhelmingly use child `Achievement.Id` values as their
  object ids, and the client progress helpers map type `7` to the checklist
  bit-count progress family (`Achievement_ProgressCategoryFromType` at
  `1406428d0`, consumed by `Achievement_GetProgressValue` at `140642b30`).
  NexusForever now feeds completed achievement ids back through the existing
  checklist manager for character and guild achievements. Type `44` remains
  deliberately blocked despite sharing the checklist progress category,
  because its checklist object ids mix achievement-like ids with item/design
  ids such as Skullcano design entries; treating all of them as child
  achievements would over-award unrelated rows. Type `15` is now implemented as
  scalar Creature2 discovery-object activation: all 29 type `15` rows in
  DataMapping carry a nonzero `ObjectId`, each resolves to `Creature2.ID`, and
  the row/object descriptions are direct interaction discoveries such as Eldan
  lockboxes, datacubes, Eldan secret projectors, the Curiositybot, SMAC pickup,
  and secret-room relic/key objects. Type `15` is not in the client checklist
  category switch, so it uses the default scalar `Data0` progress path
  (`Achievement_ProgressCategoryFromType` at `1406428d0`, consumed by
  `Achievement_GetProgressValue` at `140642b30`). NexusForever now advances
  type `15` from the shared activation-success updater used by both
  `ClientActivateUnitHandler` and `ClientActivateUnitCastHandler`, after the
  normal visibility/range/busy and activate-spell success gates, relying on
  the achievement `ObjectId` filter to restrict updates to those mapped
  Creature2 ids. Type `16` is now implemented as
  simple Creature2 activation achievements: DataMapping shows its checklist
  object ids are interactive `Creature2` ids (for example `2128` uses Auroria
  datacube creature ids `25461`-`25480`, while book/lore/social rows use the
  same Creature2-id shape), and the server owns the activation path in
  `SimpleEntity.OnActivate`/`OnActivateCast` plus the persisted datacube/journal
  managers. The trigger is therefore scoped to successful simple-object
  activation and still relies on the achievement checklist rows to select the
  eligible Creature2 ids. Type `82` is now implemented as secret-stash
  discovery on shared activation success: DataMapping has 18 type `82` rows,
  all nonzero `ObjectId` values resolve to `Creature2.ID` rows whose
  descriptions are `Secret Stash`, hidden-room, or collectable focus objects,
  and each relevant `Creature2.CreationTypeEnum` value is `8`
  (`EntityType.CollectableUnit`). The client progress helper maps type `82` to
  scalar `Data0` progress (`Achievement_ProgressCategoryFromType` at
  `1406428d0`, consumed by `Achievement_GetProgressValue` at `140642b30`).
  `CollectableUnitEntity` does not inherit `SimpleEntity`, so NexusForever now
  advances type `82` from `ClientActivateUnitHandler` after the shared
  visibility, busy, range, `OnActivateSuccess`, and activation-objective path,
  using the achievement `ObjectId` filter to restrict updates to those mapped
  Creature2 ids. Type `143` is now implemented as public-event objective
  completion: all 22 type `143` rows in DataMapping point at
  `PublicEventObjective.ID` values for public event `705` (Redmoon Terror), and
  the mapped rows cover the normal boss objectives plus the explicit
  no-death/alternate achievement objectives. The client progress helper leaves
  type `143` in the default scalar `Data0` family
  (`Achievement_ProgressCategoryFromType` at `1406428d0`, consumed by
  `Achievement_GetProgressValue` at `140642b30`). NexusForever now advances
  type `143` only when a server-owned `PublicEventObjective` transitions to
  `PublicEventStatus.Succeeded`, awarding online team members with the
  completed objective id as the achievement object id. Guild type `143` rows
  are forwarded only through the existing all-online same-guild group gate, so
  "while in a guild group" rows do not advance from solo, unguilded, or mixed
  groups. Type `104` is now implemented as targeted creature emotes:
  DataMapping has 14 checklist rows for the 12 type `104` achievements, with
  each checklist `ObjectId` resolving to a `Creature2.ID` and each
  `ObjectIdAlt` resolving to an `Emotes.ID` (`cry`, `flex`, `dance`, `laugh`,
  `wave`, `hug`, `taunt`, `applaud`, `salute`, or `pat`). The client progress
  helper maps type `104` to the checklist bit-count family
  (`Achievement_ProgressCategoryFromType` at `1406428d0`, consumed by
  `Achievement_GetProgressValue` at `140642b30`). NexusForever now advances
  type `104` from `ClientEmoteHandler` only after the emote id is validated and
  the targeted unit id resolves to a visible `IWorldEntity`, passing
  `(target.CreatureId, emote.EmoteId)` into the checklist filter. Untargeted or
  stale-target emotes therefore remain cosmetic only. The scalar achievement
  completion path now clamps
  progress at the required value and treats client rows with `Value=0` as a
  one-event requirement, which matches the many table rows that describe a
  single named action without a positive counter. After the type `45` pass, the
  offline achievement audit still leaves 41 type ids data-only:
  `9`, `12`, `13`, `14`, `22`, `26`, `33`, `44`, `46`, `55`, `57`, `61`,
  `62`, `63`, `65`, `66`, `68`, `72`, `73`, `76`, `77`, `79`, `83`, `85`,
  `86`, `87`, `88`, `89`, `94`, `96`, `101`, `102`, `109`, `112`, `114`,
  `118`, `121`, `129`, `140`, `156`, and `157`. These stay blocked because
  the corresponding event families are not server-owned yet: account/housing
  unlocks, scripted world interactions, salvage/discovery crafting, public-event
  medals, kill streak/path mission/challenge systems, PvP stats/ratings,
  taxi unlock persistence, housing neighbor/visitor/upgrades, reward tracks,
  and primal-matrix spend/unlock state.
- Galactic Archive runtime scope:
  per-character article unlock/view masks are now persisted and sent through
  the existing Galactic Archive packet models. The client mapping at
  `GalacticArchive_ApplyUnlockMaskAndDispatchEvents` (`140499a20`) supports the
  high article-unlocked bit plus lower entry bits used by the server state.
  `GalacticArchiveEntry_CalculateProgress` (`140499b40`) maps
  `ArchiveEntryUnlockRule` type `0` to achievement progress through
  `Achievement_ProgressCategoryFromType` (`1406428d0`) and
  `Achievement_GetProgressValue` (`140642b30`), type `1` to PathMission
  progress through `PathMissionRuntime_FindById` (`1403d7bc0`),
  `PathMissionRuntime_GetCurrentProgress` (`14056d0d0`), and
  `PathMissionRuntime_GetRequiredProgress` (`14056d330`), and type `2` to
  completed quest state through `QuestRuntime_GetQuestState` (`1405fbc40`).
  NexusForever now routes archive rule unlocks through achievement completion
  for type `0` and quest completion for type `2`; type `1` remains blocked
  because this server does not persist or evaluate PathMission completion state.
  The PathMission client-report side is mapped but not a completion authority:
  `PathExplorer_TrySendProgressReportOrTypeObjectProgress` (`14056fbe0`) and
  `PathExplorer_TrySendProgressReport` (`1405700f0`) scan local PathMission
  runtime objectives and send `ClientPathExplorerProgressReport` (`0x00F3`)
  when explorer-node proximity/objective checks pass. The `0x00F9`
  `ClientPathExplorerPowerMapProgress` branch at `14056fbe0` sends
  `PathMission.ObjectId` from the PathMission row offset `+0x14` for the
  type-`0x12` progress path, not `PathMission.Id`; NexusForever now names that
  parsed field `PathExplorerPowerMapId`. These reports still do not provide a
  trusted persisted PathMission-completion source for ArchiveEntryUnlockRule
  type `1`; that remains blocked until path mission progress/completion is
  server-owned and persisted.
- Rotation essence blocker:
  Quest reward type `10` is confirmed as `RotationEssence`. The active client
  reward builder at `Lua_GameQuest_BuildRewardTables` (`140665c80`) queues type
  `10` rows separately, looks up the quest's
  `WorldZone.RewardRotationContentId` through `WorldZone_GetEntry`
  (`14024db80`), and resolves the active expansion by
  `(RewardRotationContentId, Quest2.Id)`. `RewardRotation_ResolveActiveRewards`
  (`140638ad0`) reads the loaded active reward-rotation state, returns active
  essence account-currency ids, and applies the `RewardRotationModifier`
  property `38` multiplier. `QuestReward_ExpandRotationEssence` (`140667790`)
  then expands each type `10` row into a Lua AccountCurrency reward with
  `eAccountCurrencyType=<active essence currency id>` and
  `nAmount=Quest2Reward.objectAmount * activeMultiplier`.
  `RewardRotation_ApplyServerScheduleUpdate` (`140636280`) is the client-side
  schedule update consumer: it accepts server-provided 0x14-byte schedule
  entries, resolves `RewardRotationContent`, stores item/essence/modifier rows
  with expiration and granted flags, removes expired rows, and dispatches
  `RewardRotationsUpdated`. `RewardRotation_GetLoadedScheduleForContent`
  (`140636c40`) requests refreshes through `Reward_SendRewardUpdateRequest`
  (`140636ba0`, world opcode `0x07CC`) when the content-type cache expires, and
  `Lua_GameLib_GetRewardRotation` (`140709210`) /
  `Lua_GameLib_GetRewardRotations` (`140709370`) expose only those loaded
  schedules to UI. The static `Quest2Reward` rows use `objectId=0`, so a server
  grant still needs an authoritative schedule source plus the matching server
  update opcode/model before it can safely pick the currency id and multiplier.
  That opcode/model family is now partly recovered from the world-message
  registrar `FUN_14006c290`: `ServerRewardRotationScheduleArray_ReadPayload`
  (`1400a1e20`) is registered for opcode `0x07CA` with a 0x10-byte
  count-plus-pointer object and allocates `count * 0x14` before calling
  `RewardRotation_ScheduleRow_ReadPayload` (`1400a1d90`) for each row. That row
  parser reads the exact schedule shape already inferred from
  `RewardRotation_ApplyServerScheduleUpdate`: 32-bit content id, 14-bit
  reward/key id, float duration, 8-bit reward type, and trailing 32-bit value.
  `ServerRewardRotationEntryStateArray_ReadPayload` (`1400a1f80`) is
  registered for opcode `0x07C8` with the same 0x10-byte count-plus-pointer
  object and allocates `count * 0x14` before calling
  `RewardRotation_EntryStateRow_ReadPayload` (`1400a1ee0`) for each row. That
  entry-state row parser reads a 3-bit leading field, two 32-bit ids, an 8-bit
  flag/state byte, and a trailing 32-bit value, which matches the local
  entry-state buffer lane better than the schedule lane.
  The surrounding unlabeled reward cluster is now partly mapped as manager-side
  state helpers rather than candidate packet readers:
  `RewardRotation_BuildContentContextIndex` (`140635b60`) builds the local
  content lookup from `WorldZone.RewardRotationContentId`,
  `World.RewardRotationContentId`, `MatchTypeRewardRotationContent`, and
  `PublicEvent.RewardRotationContentId`; `RewardRotation_LoadEntryStateArray`
  (`140636840`) loads a sorted 0x14-byte entry-state array and replays each row
  through the shared content-refresh helper; `RewardRotation_UpsertEntryState`
  (`1406368d0`), `RewardRotation_UpdateEntryState` (`1406369c0`), and
  `RewardRotation_RemoveEntryState` (`140636ac0`) mutate one keyed row in that
  same sorted buffer and refresh or clear the affected content bucket. These
  shapes tighten the reward manager model, but they still do not expose the
  actual server reader or opcode that feeds those entry rows.
  The next helper layer under that cluster is now mapped too:
  `RewardRotation_ApplyEntryStateToLoadedContent` (`14063a0e0`) and
  `RewardRotation_ClearEntryStateFromLoadedContent` (`14063a270`) toggle the
  granted bit on matching loaded item/essence/modifier rows inside one content
  bucket; `RewardRotation_AppendScheduleEntry` (`14063a590`) and
  `RewardRotation_RemoveScheduleEntry` (`14063a640`) are the 0x18
  schedule-vector push/erase helpers used by the schedule consumer; and
  `RewardRotation_AppendEntryStateRows` (`14063a760`),
  `RewardRotation_InsertEntryStateRow` (`14063a810`),
  `RewardRotation_SortEntryStateRows` (`14063a8f0`),
  `RewardRotation_HasEntryStateFlag` (`14063aa90`), and
  `RewardRotation_RemoveEntryStateRow` (`14063abc0`) manage the sorted 0x14
  local entry-state buffer. `RewardRotation_ClearContentContextIndex`
  (`14063a490`) tears down the per-content lookup and its loaded schedule
  vectors. This confirms the whole mapped helper layer is still local
  state/index maintenance rather than the missing network reader.
  A separate callback-table trace now resolves the higher Lua consumer too:
  `Lua_GameMatchMakingEntry_GetRotationRewards` (`14073c340`) sits in the
  `Game.MatchMakingEntry` method table beside the string
  `GetRotationRewards` (`140b488d8`) and is the only recovered direct caller of
  `RewardRotation_BuildLuaRotationRewards` (`140639060`). That builder emits
  the `bRewardsLocked`/`arRewards` Lua fields from already-loaded schedule
  state, and it fans out through
  `RewardRotation_BuildLuaItemReward` (`140639710`),
  `RewardRotation_BuildLuaEssenceReward` (`140639c80`), and
  `RewardRotation_BuildLuaModifierReward` (`140639e60`). This cleanly separates
  the mapped MatchMakingEntry/UI serialization lane from the still-unmapped
  server response reader.
  A follow-up allocator trace for `FUN_1403374e0` found one unrelated 0x14-byte
  array reader at `FUN_14009f6b0`, but the actual reward reader family is now
  recovered through raw immediate searches instead of direct xrefs. The reward
  manager consumers still show only unwind/data references when traced as
  functions, but the authoritative opcode registrations now live in
  `FUN_14006c290`: the one-row entry-state reader thunk at non-function
  address `1400a2040` validates `R8` and tail-jumps into
  `RewardRotation_EntryStateRow_ReadPayload` (`1400a1ee0`), and that 0x14-byte
  reader is registered for opcodes `0x07CB`, `0x07C9`, and `0x07C7`. This
  tightens the blocker substantially: the client request opcode (`0x07CC`), the
  schedule-array load opcode (`0x07CA`), the entry-state-array load opcode
  (`0x07C8`), and the three single-row entry-state delta opcodes
  (`0x07CB`/`0x07C9`/`0x07C7`) are now known. The remaining open question is the
  exact semantic split between those three delta opcodes - which one feeds
  `RewardRotation_UpsertEntryState`, `RewardRotation_UpdateEntryState`, or
  `RewardRotation_RemoveEntryState`. No static or random essence grant was
  added.
  Follow-up implemented from this pass: NexusForever now defines writable
  server message models for the mapped reward-rotation schedule array
  (`0x07CA`), entry-state array (`0x07C8`), and three single-row entry-state
  delta opcodes (`0x07C7`, `0x07C9`, `0x07CB`) using the field widths recovered
  from `RewardRotation_ScheduleRow_ReadPayload` (`1400a1d90`) and
  `RewardRotation_EntryStateRow_ReadPayload` (`1400a1ee0`). The three delta
  model names remain conservative/inference-backed; no reward-rotation schedule
  source, static grant, random essence grant, or entry-state mutation service was
  added.
  A follow-up Ghidra pass on `0x07CD` now maps the remaining fixed-size reward
  packet in the same registrar cluster: `ServerRewardRotationContentContext_ReadPayload`
  (`14008fcb0`) is registered for opcode `0x07CD` with `R8D=0x28` and reads a
  14-bit reward-rotation index, four 32-bit fields, a counted 32-bit content-id
  array, and a trailing 1-bit flag. The adjacent `Server0x07D3_ReadPayload`
  (`14008fdc0`) is a separate opcode (`0x07D3`, `R8D=0x10`) that reads a 32-bit
  count plus `count * 0x28` rows through the same row reader. NexusForever now
  models `0x07CD` as `ServerRewardRotationContentContext`; the middle three
  uint32 field meanings and the consumer that applies the context row remain
  unresolved.
  Consumer correlation from this pass:
  `RewardRotation_BuildContentContextIndex` (`140635b60`) rebuilds the client
  manager hash at `manager + 0x118` from the same game-table sources
  (`WorldZone`, `World`, `MatchTypeRewardRotationContent`, `PublicEvent`, and
  `RewardRotationContent.ContentTypeEnum`) that NexusForever now uses in
  `RewardRotationContentContextBuilder`. `RewardRotation_ApplyServerScheduleUpdate`
  (`140636280`) then resolves schedule rows through
  `RewardRotationContent.ContentTypeEnum * 0x20 + manager + 8`, matching the
  seven throttled indices in `Reward_SendRewardUpdateRequest` (`140636ba0`).
  Opcode registration for `0x07CD` passes a null handler pointer (`RSP+0x20 = 0`)
  like `0x07CA`/`0x07C8`, so the apply function is reached only through the same
  runtime dispatch table that already references `RewardRotation_ApplyServerScheduleUpdate`
  by data (`140bf449c`). No static code xref names the `0x07CD` apply helper yet.
  NexusForever now models `0x07D3` as `ServerRewardRotationContentContextArray`
  (count plus `count * 0x28` rows via the same row reader) and chunks game-table
  content ids across `0x07CD` or `0x07D3` when a refresh index exceeds the
  five-id wire budget. `ClientRewardUpdateRequestHandler` uses
  `GameTableRewardRotationRefreshProvider` for content-context plus schedule packets.
  Schedule duration is now mapped from `RewardRotation_ApplyServerScheduleUpdate`
  (`140636280`): wire `Duration` is a float day count converted to FILETIME expiry
  (`days * 864000000000` plus fractional day remainder). NexusForever derives days
  from `GameFormula` `821` `Dataint0` hours / 24 (48h ? `2.0f` days), correlated with
  the auction-house duration formula. `RewardRotationScheduleBuilder` emits one
  item/essence/modifier row per content id via deterministic global-catalog selection
  because client tables expose no `RewardRotationContent`?reward linkage. Entry-state
  wire semantics are now mapped (`RewardRotation_ApplyEntryStateToLoadedContent`
  `14063a0e0`, `RewardRotation_HasEntryStateFlag` `14063aa90`): `State` is the
  reward-type lane (1/2/3) and `Value` is a grant bitmask (`0x1` item/modifier,
  `0x80000000` essence). NexusForever keeps `0x07C8` empty for fresh accounts
  until account grant persistence exists; `RewardRotationEntryStateBuilder` can
  emit grant rows when needed. `0x07CD` `UInt0`/`UInt1`/`UInt3` are populated
  with correlated throttle defaults from `FUN_140635840` (`manager + 0x150 +
  index * 0x14`, 1000 ms); the dedicated apply helper and `Flag` bit remain blocked.
- Taxi unlock persistence blocker:
  Type `101` achievements (`4714`/`4715`, `Making Connections`) remain
  mapped-only. The client can receive an authoritative unlocked flight-path
  list through opcode `0x0188`, parsed by `ServerFlightPathUpdate_ReadPayload`
  (`14008eaa0`) as a 32-bit count followed by raw 32-bit ids. The flight-path
  purchase flow calls the newly labelled
  `FlightPath_IsNodeUnlockedOrAvailable` (`1404ad9b0`), which checks a loaded
  node tree at manager offset `+0x110` before falling through static TaxiNode
  flags or the taxi prerequisite helper. The rapid transport flow now has
  `RapidTransport_IsNodeUsableForPlayer` (`1404ada70`), which gates node
  type/faction/static flags and then calls the same prerequisite helper with a
  rapid-transport mode; `RapidTransport_CanUseNode` (`1404adfe0`) also checks
  world/range constraints before sending opcode `0x0141`. NexusForever has
  packet models and purchase/rapid-transport handlers, but no character-owned
  TaxiNode/FlightPath persistence model, no initial `ServerFlightPathUpdate`
  population, and no trusted unlock mutation path. Implementing type `101` or
  unlocking taxi nodes from use would therefore overreach the mapped server
  state; it stays blocked until a persisted unlock table and server-owned
  discovery/unlock rule are added and verified.
- Crafting hot/cold discovery finish mapping:
  opcode `0x0853` is now labelled as `ServerCraftingFinish_ReadPayload`
  (`1400a4560`). The client reads one pass bit, 15-bit crafted
  `TradeskillSchematic2` id, 18-bit crafted `Item2` id, a raw unused uint,
  32-bit `CodeEnumCraftingDiscoveryHotCold`, 4-bit direction, earned XP, and a
  counted returned-material id array. `Crafting_HandleServerCraftingFinish`
  (`1405e6690`) consumes the decoded payload and dispatches the
  `CraftingDiscoveryHotCold` client event only when the craft passed and the
  hot/cold enum is not `Success` (`3`). The `CodeEnumCraftingDiscoveryHotCold`
  registration in `Lua_RegisterCraftingBindings` (`1405a3000`) confirms
  `Cold=0`, `Warm=1`, `Hot=2`, and `Success=3`. NexusForever's fixed-recipe
  success path now writes `CraftingDiscovery.Success`, because that path does
  not yet own discovery-coordinate math and should not emit a false `Cold`
  discovery result. The actual hot/cold schematic discovery mechanic remains
  blocked: `Lua_Crafting_GetSchematicInfo` (`140599010`) and
  `Lua_Crafting_AddCoordinateDiscoveryInfo` (`14059e9a0`) show the UI-side
  discovery fields (`fRadius`, `fVectorX`, `fVectorY`, `fDiscoveryAngle`,
  `fDiscoveryDistanceMin`, `fDiscoveryDistanceMax`, `bIsUndiscovered`) being
  derived from `TradeskillSchematic2` data and player modifier state, but the
  authoritative craft-stat coordinate interpretation and discovery-unlock
  mutation rule are still unmapped.
- Durable rune state blocker:
  the client-to-server rune request packet family remains mapped (`0x0859`
  add slot, `0x085A` clear, `0x085B` install, `0x085C` reroll), and
  `RuneCrafting_SendClientRuneInstall` (`14059d250`) validates the local item
  rune array before sending item-guid plus rune Item2 ids. NexusForever now
  validates those requests and mutates a runtime-only `Dictionary<ulong,
  List<RuneSlotState>>`, including rune item consume/recover behavior. The
  durable side is still blocked: `ItemModel` persists only item id, location,
  stack/charges, durability, expiration, and soulbound state; the item packet
  has opaque `RandomGlyphData`/`Glyphs` fields, but the slot-type packing and
  installed-rune serialization are not mapped to a database-owned item model.
  Persisting or broadcasting rune state without that mapping would risk
  corrupting item serialization or losing runes across save/load.
- Harvesting blocker:
  `Harvest_DispatchItemsSentToOwnerEvent` (`1404bbb80`) is now labelled as the
  client consumer that builds `{ item, nCount }` Lua rows and dispatches
  `HarvestItemsSentToOwner` from a server harvest-item payload. The payload
  shape proves the client can display item/count harvest results, while
  `TradeskillHarvestingInfo.tbl` only supplies tier/prerequisite/minimap
  metadata. NexusForever currently has `HarvestUnitEntity`/`HousingHarvestPlug`
  entity shells and group `HarvestLootRule` settings, but no server-owned
  harvest interaction handler, no authoritative node depletion/respawn state,
  and no mapped material/XP grant path. Harvesting stays blocked until those
  server mutations and the outbound harvest-result packet/model are mapped
  together.
- Quest cap and tradeskill naming follow-up:
  `QuestPrerequisite_CanAccept` (`140552550`) reads `GameFormula` id `0x28f`
  (`655`), falls back to `0x28` (`40`) when the row is absent, and returns the
  prerequisite failure once the current quest count is greater than or equal to
  that limit around `exports\WildStar64.exe\selected_decompiled.c:25105`.
  Local MySQL confirms `wildstar_client.GameFormula` row `655` has
  `Dataint0=40`. NexusForever now mirrors the inclusive client gate through
  `QuestManager.HasActiveQuestCapacity`, so non-contract quest acceptance stops
  at exactly the client limit instead of allowing one extra active quest.
  The same audit pass confirmed `wildstar_client.Tradeskill` id `16` resolves
  through `stringsenus` to `Technologist`, matching
  `Tools\DataMapping\output\tradeskill_client_map.csv`; the server enum now
  uses `Technologist = 16` instead of the stale `Augmentor` name.
  Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -v minimal --nologo` passed with `56` tests, and
  `python Tools\WikiArchiveAudit\audit_wildstar_wiki.py --domain tradeskills
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18` passed with
  tradeskill id `16` shown as `Technologist` for both client and server.
  `Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch` reported the
  `WildStar64.exe` export manifest as `ok`.

### Reward rotation entry-state delta opcodes (0x07C7, 0x07C9, 0x07CB)

Opcodes `0x07C8` and `0x07CA` were confirmed by their registered reader labels
(`ServerRewardRotationEntryStateArray_ReadPayload` at `1400a1f80` and
`ServerRewardRotationScheduleArray_ReadPayload` at `1400a1e20`).  The three
remaining delta opcodes (`0x07C7`, `0x07C9`, `0x07CB`) all register the same
null-guard thunk at `1400a2040` as their reader, which tail-calls
`RewardRotation_EntryStateRow_ReadPayload` at `1400a1ee0`.

The three consumer functions for the delta opcodes are confirmed at addresses
`1406368d0` (`RewardRotation_UpsertEntryState`), `1406369c0`
(`RewardRotation_UpdateEntryState`), and `140636ac0`
(`RewardRotation_RemoveEntryState`).

The mapping of opcode -> consumer is **inference-level** (Level 2 on the
evidence ladder).  Every static analysis path was exhausted before falling back
to ordering-based inference:

- All three delta opcodes register identical reader/size/callback args -
  no discriminating static field.
- `FindAllCallsAndJumpsToTargets.java` (full `0x140001000`-`0x140957000` scan)
  found **zero** direct CALL or JMP instructions to any consumer function.
- A full PE section search found **zero** 8-byte absolute pointer values for
  any consumer address; only `.pdata`/`.rdata` xdata BeginAddress RVAs appear.
- There are no EntryState-keyed wide-string event names in `.rdata`.
- The consumer functions are reached exclusively via runtime heap-allocated
  function pointer tables - static analysis cannot trace the path.

**Inferred mapping** (standard WildStar CRUD opcode ordering, consistent with
adjacent confirmed opcodes `0x07C8`/`0x07CA`):

| Opcode   | Consumer function                    |
|----------|--------------------------------------|
| `0x07C7` | `RewardRotation_UpsertEntryState`   |
| `0x07C9` | `RewardRotation_UpdateEntryState`   |
| `0x07CB` | `RewardRotation_RemoveEntryState`   |

All five opcodes (`0x07C7`-`0x07CB`) have been added to `GameMessageOpcode.cs`
and corresponding `IWritable` server message model classes have been created in
`Source\NexusForever.Network.World\Message\Model\`.  The consumer function
descriptions in `function_labels.csv` have been annotated with the inferred
opcode mapping.  A full reward rotation manager (send-side integration) is a
separate follow-up task.

Reward-rotation table registration follow-up:

- Houston and WildStar now both have durable client DB registration labels for
  `MatchTypeRewardRotationContent`, `RewardRotationContent`,
  `RewardRotationItem`, `RewardRotationEssence`, and
  `RewardRotationModifier`. The functions are mapped by exact table descriptor
  strings and `DB\*.tbl` paths in the selected exports.
- This improves the reward-rotation table evidence trail only. It does not
  unblock live schedule generation: the static table definitions identify
  content/reward catalogs, but no server-owned schedule timing or entry-state
  mutation source is proven by these registration functions alone.

## Practical Next Steps

1. Keep extending `Decomp\Analysis\function_labels.csv` as functions are
   confirmed. Re-run `.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly`
   so the exported C becomes progressively easier to read.
2. Cross-check constants and field reads in the selected C against
   `GameMessageOpcode.cs`, the STS models, and generated `GameTable` models.
3. Add narrower high-value patterns to `ExportNexusForeverAnalysis.java` when a
   new subsystem becomes the focus.

### `Game_Spell_IsSelfSpellDelegate` target-type bitmask decode (`1403b4ec0`)

The 56-byte `Game_Spell_IsSelfSpellDelegate` function is now fully decoded.
It is called by the `Lua_GameSpell_IsSelfSpell` path (`1405ee4a0`) after the
spell-service wrapper resolves the active spell metadata.

**Decoded pseudocode:**
```c
undefined8 Game_Spell_IsSelfSpellDelegate(longlong param_1) {
  longlong lVar2 = *(longlong *)(param_1 + 0x70);  // ptr to target-mechanics runtime object
  uint uVar1    = *(uint *)(lVar2 + 0x7c);         // TargetType (uint)
  if (((7 < uVar1) || ((0x85U >> (uVar1 & 0x1f) & 1) == 0)) &&
     ((*(int *)(lVar2 + 0x18) != 3 ||
      ((*(int *)(lVar2 + 0x9c) != 0 || ((uVar1 - 4 & 0xfffffffb) != 0)))))) {
    return 0;   // NOT a self-spell
  }
  return 1;     // IS a self-spell
}
```

**Bitmask `0x85U = 10000101b`:** bits 0, 2, 7 set ?
TargetTypes 0, 2, and 7 are all "self-spell" per the client UI.

**Additional self-spell case:** TargetType 4 (or 8) with
`lVar2 + 0x18 == 3` (shape field) AND `lVar2 + 0x9c == 0` (constraint field).

**Runtime wrapper field offsets (within target-mechanics runtime object at param_1+0x70):**

| Offset | Type  | Meaning                               |
|--------|-------|---------------------------------------|
| +0x18  | int   | Shape/AOE type (3 = special gate)     |
| +0x7c  | uint  | TargetType (main dispatch value)      |
| +0x9c  | int   | Constraint field (0 = gate passes)    |

**IsSelfSpell semantics vs. server ResolvePrimaryTargetId:**

`IsSelfSpell` is a Lua UI function that determines whether a targeting cursor
is shown.  TargetType 7 returning "self-spell" means the UI auto-fires at the
current combat target without a manual aim (auto-attack behavior).  The server
`ResolvePrimaryTargetId` must NOT blindly mirror `IsSelfSpell`:

| TargetType | IsSelfSpell | Server ResolvePrimaryTargetId        |
|------------|-------------|--------------------------------------|
| 0          | true        | return `Owner.Guid` (self)           |
| 2          | true        | return `Owner.Guid` (already done)   |
| 7          | true (UI)   | return `Owner.TargetGuid` if visible |

**Evidence witnesses (from `Spell4TargetMechanics` + spell4 joins):**

- TargetType 0, mechanic 1: spell4=305 ("Q388 anti-tank mine explosion") - self-centered explosion.
- TargetType 7, mechanic 25: spell4=339 (auto-attack) - fires at current enemy target.
- TargetType 7, mechanic 18: spell4=26813 (client test AE) - AE shape.
- TargetType 6, mechanic 17: spell4=11820 ("[TEST] Recharge Item Batteries Full") - item-activation. Later `SpellTarget_ResolveTargetEntity` evidence supersedes the earlier blocker and confirms caster/self resolution.

**Server implementation applied** (`CharacterSpell.ResolvePrimaryTargetId`):
- Added constants `TargetTypeNoExplicitTarget = 0u` and `TargetTypeServiceLookup = 7u`.
- TargetType 0 now returns `Owner.Guid` (merged with TargetTypeSelfAoe branch).
- TargetType 7 now falls through to the current-target path (like SingleTarget/TargetAoe/Chain).
- TargetType 6 now returns `Owner.Guid` after the later `SpellTarget_ResolveTargetEntity` decode confirmed caster/self resolution.

### `SpellCast_ResolveTargetsAndValidate` target-validation flow decode (`1403988d0`)

The 1005-byte `SpellCast_ResolveTargetsAndValidate` function is the shared spell-cast
target-preparation helper that resolves target entity IDs and runs range/validity checks.

**Key behaviours decoded:**

- **Trade-partner intercept** (`param_1 + 0x6718 != 0`): If the caster is in an active
  trade and the target entity GUID matches the trade partner at `DAT_140c65898 + 0x78` or
  `DAT_140c65898 + 0x6490`, the function sends opcode `0x18F` (399) as a trade-partner
  cast notification.

- **TargetType 2 (SelfAoe) fast path**: An early branch fires when
  `TargetType == 2` AND `wrapper+0x10c` bit `0x4000000` is NOT set AND
  `wrapper+0x108 != 0`. Calls `FUN_140398800` (heavy validator) directly
  rather than going through `SpellTarget_ResolveTargetEntity`. This confirms
  that the server's "always return `Owner.Guid` for type 2" logic is
  consistent with the client intent for normal self-AOE casts.

- **General target path**: If `*param_5 == 0`, calls
  `SpellTarget_ResolveTargetEntity(param_1, spell_entity, spell_wrapper, 0, ...)`.
  This is the labelled function at `14055bdc0` (770 bytes, **not yet decompiled**).
  It is the definitive target-resolution function that enforces all target masks,
  range, facing angle, and valid-target category checks.

- **Position-AOE (TargetType 4) storage pattern** (from `SpellCast_ValidateAndDispatch`
  at `1403998e0`): TargetType 4 pending cast is written to
  `param_1 + 0x6ce8` (spell wrapper ptr), `+0x6cf0` (flags pair `0x100000000`),
  `+0x6cf8` (explicit position data), `+0x6d00` (state word `9`). The server
  mirrors this by storing `Parameters.Position` before dispatching.

- **Known error codes** (`int` return):

| Code | Hex   | Meaning (inferred from context)             |
|------|-------|---------------------------------------------|
| 0    | 0x00  | Success                                     |
| 5    | 0x05  | Service check failed (no service node)      |
| 0x11 | 17    | Prerequisite failed                         |
| 0x16 | 22    | No spell object resolved                    |
| 0x1e | 30    | No local player entity                      |
| 0x33 | 51    | Trade-loop break condition                  |
| 0x43 | 67    | Error code (triggers trade-partner re-check)|
| 0x45 | 69    | Error code (same trade-partner re-check)    |
| 0x69 | 105   | Early bail from self-AOE fast path          |
| 0x119| 281   | Payload `uStack_130 = 1`                    |
| 0x13d| 317   | Success-equivalent in trade path            |

**Important unresolved function:** `SpellTarget_ResolveTargetEntity` at `14055bdc0`
(770 bytes) is the entity-lookup + valid-target-category enforcement entry point.
Adding it to the Ghidra selection is the next step for decoding non-dead valid-target
masks, facing-angle gates, and range-check subtleties.

### `UnitState_*` packet structure decode (`1403db7f0`-`1403db920`)

Three labelled functions near `0x1403db7f0` decode the server `UnitState` packet
structure and entity state fields:

**`UnitState_UpdateCachedStateFields` (`1403db7f0`):**
```c
// param_2 = int[3]: [0]=entity_guid, [1]=new_state_value, [2]=secondary_state_data
longlong entity = FUN_1403d90d0(param_1, param_2[0]);
FUN_14045bdc0(entity, param_2[1], param_2[2]);
// If entity is NOT the local player:
if (entity + 0x108 != param_2[1]) {
    entity + 0x108 = param_2[1];  // current unit state value
    entity + 0x10c = FUN_14045a950(entity);  // derived state flags
}
```
**Entity fields confirmed:**
- `entity + 0x108`: current `UnitState` value (also used as spell-wrapper targeting flags)
- `entity + 0x10c`: derived flags computed from state; bit `0x02` = valid interactable target;
  bit `0x4000000` = self-AOE gate

**`UnitState_ApplyResolvedState` (`1403db870`):**
```c
// param_2 = uint[4]: [0]=entity_guid, [1]=state_value, [2]=?, [3]=dismount_flag
if (entity == local_player && param_2[1] == 0) {
    // Clear player state: resets param_1+0x1b80 and param_1+0x71a0+0x54
}
FUN_14045e740(entity, param_2[1]);  // applies the state
if (entity.mounted && param_2[1] != 0) return;
if (entity.mounted && param_2[3] != 0) trigger_dismount();
```

**`UnitState_MaybeDispatchUnitEvaded` (`1403db920`):**
- State value **4** = `"UnitEvaded"` ? fires client event `"UnitEvaded"` with entity GUID.
- This is the client-side cosmetic evade-float-text trigger.

---

## SpellTarget_ResolveTargetEntity Decode (14055bdc0, 770 bytes)
### Run: 350-function Ghidra export (ranked #287 among labeled)

### Function Signature and Purpose
`
SpellTarget_ResolveTargetEntity(longlong player, longlong caster, longlong spell_ctx, int explicit_id, int current_target_id)
`
Resolves the entity reference to use for a spell's primary target. Returns a world-entity pointer via
FUN_1403d90d0(player, resolved_id).

### TargetType Entity Switch Table (primary dispatch)
`c
if (wrapper+0x18 == 3) {
    param_5 = 0;  // shape-type 3 = no entity (position-AOE)
} else {
    switch (wrapper+0x7c) {  // TargetType
    case 0:  // NoExplicitTarget (mine explosion)
    case 2:  // SelfAoe
    case 6:  // ItemActivation
    case 7:  // ServiceLookup / auto-attack
        param_5 = *(int *)(caster + 8);  // caster entity ID = self
        break;
    case 1:  // SingleTarget
    case 3:  // TargetAoe (target-centered)
    case 5:  // (unknown label)
    case 8:  // (unknown label)
        if (param_5 == 0)
            param_5 = *(int *)(caster + 0x108);  // entity's current target
        break;
    default:  // case 4 = PositionAoe
        param_5 = 0;  // no entity, position only
    }
}
`

### Key Semantics
- **Entity resolution is for telegraph anchoring / UI, not final damage target.**
  Type 7 (auto-attack) returns the caster entity here; server-side damage still targets Owner.TargetGuid.
- **TargetType 6 (ItemActivation) is confirmed self-targeting**: client groups it with 0/2/7.
- **TargetTypes 5 and 8 are confirmed single-target category** (use current target if none explicit).

### Auto-Target Fallback (bitmask 0x12a)
When xplicit_id == 0 AND
esolved_id == 0 AND TargetType is in bitmask x12a:
- x12a = 100101010b ? bits 1, 3, 5, 8 ? TargetTypes 1, 3, 5, 8 get auto-target fallback.
- Searches nearby valid entities within ~5 yards (config entry 0x145 ? lVar4+0x18, default 5.0f).
- If found and within range: calls TargetSelection_ApplySelectionAndDispatch(player, entity_id) to select that entity.

### Entity Field Offsets (confirmed)
| Offset | Content |
|--------|---------|
| ntity + 0x08 | entity GUID / ID (uint) |
| ntity + 0x108 | current UnitState value |
| ntity + 0x11e0 | x position (float) |
| ntity + 0x11e4 | y position (float) |
| ntity + 0x11e8 | z position (float) |
| ntity + 0x16f0 | bounding-sphere object pointer (call +0x50 to get sphere; radius at +0x30) |

### Special Override Path (flags param_1+0x7ba0 bit 0)
When player+0x7ba0 & 1 AND wrapper+0x10c bit 26 AND wrapper+0x10c bit 28:
- Reads secondary entity ID from player+0x6490 ? +0x108.
- Performs relationship check via FUN_14045a950 and bounding check via FUN_140466b90.
- Result: overridden entity replaces normal entity target.

### Server Implementation Applied
In CharacterSpell.ResolvePrimaryTargetId():
- **TargetType 6**: now returns Owner.Guid (previously returned u; client evidence confirms self-targeting).
- **TargetTypes 5 and 8**: now included in the "use current target" path (were falling through to u).
- TargetType 7 server-side still returns Owner.TargetGuid when the selected target is visible; the client-side IsSelfSpell flag is a UI auto-fire/cursor rule, not a server self-targeting rule.

---

## SpellService Type-7 Service Tree and Spell Wrapper Hash Map (800-function export)

### Functions decoded
- SpellService_LookupSpellWrapperById @ 1407a0fd0
- SpellService_ResolveSpellWrapper @ 1403acd90
- Lua_GameSpell_IsSelfSpell @ 1405ee4a0
- Lua_GameSpell_IsFreeformTarget @ 1405ee640

### Spell Wrapper Hash Map (service_obj + 0x540)

SpellService_LookupSpellWrapperById is a chained hash map lookup at spell_service_obj + 0x540:
- +0x08 = bucket count (ulonglong)
- +0x10 = bucket array pointer
- +0x18 = hash function (code pointer)
- +0x20 = key equality function (code pointer)
- Bucket chain nodes: [hash64, 64bit-next, key_uint, value_spell_wrapper_ptr]
- Returns
ode + 0x18 (4th 8-byte element) = spell wrapper pointer, or 0 if not found

SpellService_ResolveSpellWrapper wraps this:
1. If param_3 == player_entity (DAT_140c65898 + 0x78) or current_target (+0x6490), try context-aware path via FUN_1405a5b90
2. Otherwise falls back to SpellService_LookupSpellWrapperById hash map

### Service Tree (service_obj + 0x788)

For **TargetType 7** spells, a per-spell override binary tree at service_obj + 0x788:
- Balanced BST (std::map style / red-black tree variant)
- 	ree_root = *(service_obj + 0x788) - sentinel/root node
-
oot + 0x08 = first real node (tree start)
- Tree node structure:
  -
ode + 0x10 = right child pointer
  -
ode + 0x18 = left child pointer
  -
ode + 0x20 = key: spell4 ID (uint)
  -
ode + 0x28 = pointer to service spell array
- Traversal: classic BST lower_bound on spell4 ID
- Service spell array: rray[0] = first service entry, rray[0] + 4 = spell4 ID of service spell

When found, calls SpellService_ResolveSpellWrapper on rray[0] + 4 to get a secondary wrapper, then reads its flags or calls Game_Spell_IsSelfSpellDelegate on it.

### Spell4 PropertyFlags bits confirmed (at spell_wrapper + 0x70 + 0x108)

| Bit | Hex | Server constant | Meaning |
|---|---|---|---|
| 6 | x00000040 | (SpellTargetingFlags.InterruptOnMove) | IsMovingInterrupted |
| 22 | x00400000 | SpellTargetingFlags.FreeformTarget | IsFreeformTarget |
| 29 | x20000000 | SpellPropertyFlags.HasServiceTokenCost | ServiceToken cast behavior |

Note: The server reads IsFreeformTarget and IsMovingInterrupted from Spell4BaseEntry.TargetingFlags (via SpellBaseInfo), not Spell4Entry.PropertyFlags. Both use the same bit value x400000, confirming the mapping is consistent across tables.

### IsSelfSpell bitmask confirmation

Game_Spell_IsSelfSpellDelegate checks spell_wrapper + 0x70 + 0x7c (TargetType) against bitmask x85:
- x85 = 10000101b ? TargetTypes **0** (NoExplicitTarget), **2** (SelfAoe), **7** (ServiceLookup) ? IsSelfSpell = true
- Additional rule: shape-type +0x18 == 3 AND +0x9c == 0 AND TargetType ? {4, 8} ? also self
- IsSelfSpell = true means the UI auto-casts without a targeting cursor; no change to server ResolvePrimaryTargetId

### Service tree lookup in IsSelfSpell / IsFreeformTarget

Both Lua_GameSpell_IsSelfSpell and Lua_GameSpell_IsFreeformTarget use the same tree:
1. Check if inner struct field [6] (at +0x18) == 7 (TargetType ServiceLookup)
2. Get spell4 ID from *inner_struct (first uint)
3. BST lower_bound search for spell4 ID
4. If found: resolve service spell wrapper and call delegate on it
5. If not found: fall through to standard bitmask/flags check
## Entity Spell Wrapper BST and Target Resolution Functions

### Functions decoded (InspectCodeAddress pass, May 2026)

This section documents three closely related target-validation functions discovered
by direct `InspectCodeAddress.java` decompile.

---

### Entity_LookupSpellWrapperInBST @ 1405a5b90

Performs a BST lower_bound lookup to find a spell wrapper by spell4 ID on a
specific entity object.

```c
undefined8 Entity_LookupSpellWrapperInBST(longlong entity, uint spell4_id) {
    longlong root = *(longlong *)(entity + 0x7d18);  // map header sentinel
    longlong candidate = root;
    longlong node = *(longlong *)(root + 8);         // root->_M_parent (first real node)
    while (node != 0) {
        if (*(uint *)(node + 0x20) < spell4_id) {
            node = *(longlong *)(node + 0x18);       // go right
        } else {
            candidate = node;
            node = *(longlong *)(node + 0x10);       // go left, track candidate
        }
    }
    if ((candidate == root) || (spell4_id < *(uint *)(candidate + 0x20))) {
        candidate = root;  // no exact match found
    }
    if (candidate != root) {
        return *(undefined8 *)(candidate + 0x28);    // return spell_wrapper ptr
    }
    return 0;
}
```

**Per-entity spell wrapper BST (at `entity + 0x7d18`):**
- `entity + 0x7d18` = pointer to `std::map<spell4_id, spell_wrapper*>` header
- Map sentinel node: `+0x08` = root node
- BST node layout:
  - `+0x10` = left child
  - `+0x18` = right child
  - `+0x20` = key: spell4 ID (uint)
  - `+0x28` = value: spell_wrapper pointer

This is a per-entity cache of spell wrappers distinct from the global spell
service hash map at `service_obj + 0x540`. When a spell targets the local player
or the player's current target, the client uses this entity-level lookup rather
than the global hash map.

**Called by:** `SpellService_ResolveSpellWrapper @ 1403acd90` when `param_3` is
the local player entity (`DAT_140c65898 + 0x78`) or current target (`+0x6490`).

**Server implication:** No change needed. The server resolves spell wrappers from
the GameTable entries directly. The entity-level BST is a client-side optimization
for rapid per-entity lookups and does not affect server target selection logic.

---

### SpellTarget_ResolveAndValidateWrapper @ 140398800

Gate function that resolves the spell wrapper for a candidate target and, on
success, populates result fields and calls the downstream validator.

```c
undefined8 SpellTarget_ResolveAndValidateWrapper(
    undefined8 context, int *spell_result, longlong target_entity,
    longlong extra_ctx, int *extra_ptr, undefined8 p6, undefined8 p7)
{
    if (*spell_result != 0) {
        if (target_entity != 0) {
            bool is_self_or_target =
                (*(longlong *)(DAT_140c65898 + 0x78) == target_entity) ||
                (*(longlong *)(DAT_140c65898 + 0x6490) == target_entity);
            longlong wrapper = 0;
            if (is_self_or_target) {
                wrapper = DAT_140c65898;
            }
            if (wrapper != 0) {
                wrapper = Entity_LookupSpellWrapperInBST(wrapper, spell4_id);
                if (wrapper != 0) goto found;
            }
        }
        wrapper = FUN_1407a0fd0(DAT_140c65b70);  // global fallback lookup
        if (wrapper != 0) {
found:
            if (extra_ctx != 0) spell_result[9] = *(int *)(extra_ctx + 8);
            if (extra_ptr != null) {
                spell_result[0xd] = extra_ptr[0];
                spell_result[0xe] = extra_ptr[1];
                spell_result[0xf] = extra_ptr[2];
            }
            return FUN_1403ace00(extra_ptr, spell_result, p6, p7);
        }
    }
    return 4;  // error: no valid spell wrapper
}
```

**Key observations:**
- Returns `4` when `*spell_result == 0` or no wrapper found (error code 4)
- Local player is at `DAT_140c65898 + 0x78`; current target at `DAT_140c65898 + 0x6490`
- Entity-level BST lookup takes priority over the global fallback
- Populates `spell_result[0xd/0xe/0xf]` from `extra_ptr` (likely world position)
- `spell_result[9]` gets a value from `extra_ctx + 8` (likely the spell4 ID)

---

### SpellTarget_ValidateTargetRelationship @ 1403b44b0

Validates that a spell target meets relationship and alive requirements.

```c
undefined8 SpellTarget_ValidateTargetRelationship(
    undefined8 p1, int expected_faction, int target_mask,
    longlong target_entity, longlong has_faction_check, undefined8 caster_ctx)
{
    if (has_faction_check != 0) {
        int relation = FUN_14046c580(target_entity, caster_ctx);  // GetRelation
        if (relation == expected_faction) return 0;  // same group - OK
        if ((*(byte *)(target_entity + 0x24) & 1) != 0) {  // target is dead
            if (target_mask == 0) return 0x59;  // error: dead target
            goto validate_mask;
        }
    }
    if (target_mask == 0) return 0;
validate_mask:
    // vtable dispatch through FUN_1403b4a10 and FUN_1403b4a20
    return FUN_1403b4a20(&vtable, FUN_1403b4a10(&vtable, target_mask));
}
```

**Key observations:**
- `target_entity + 0x24` byte bit 0 = dead flag (entity alive/dead state)
- Error code `0x59` (89 decimal) = dead-target rejection
- `FUN_14046c580` = entity relationship query (faction/group comparison)
- `expected_faction` = 0 means self-group check; if target is in same faction as
  caster the validation short-circuits to success (return 0)
- `target_mask == 0` = no further type-check needed; non-zero triggers vtable
  dispatch to `FUN_1403b4a10`/`FUN_1403b4a20` for deeper interactable/relation checks

**Entity dead flag:** `entity + 0x24` bit 0 = dead.
- This is a client-side dead marker distinct from health == 0 checks.

**Server implication:** Server already validates dead targets separately; no server
change needed. The `0x59` error code maps to a client-side `SpellCastFailed`
result that is not surfaced to the server.

---

### Channel Data Struct Confirmation (`Lua_GameSpell_GetChannelData @ 1405ed640`)

The channel data struct at `spell_wrapper + 0x50` (decoded from GetChannelData):

| Offset | Type | Meaning |
|--------|------|---------|
| `+0x00` | uint | `fInitialDelay` in milliseconds (returned as `/ 1000.0` seconds) |
| `+0x04` | uint | `fMaxTime` in milliseconds (returned as `/ 1000.0` seconds) |

The Lua method returns both values as seconds. The server uses
`Spell4Entry.CastTime / 1000d` for execute timing - this is the game-table
equivalent of `fMaxTime` from the runtime wrapper. No server change needed.

---

### Server Implementation Status

| Function | Address | Server Status |
|----------|---------|---------------|
| Entity_LookupSpellWrapperInBST | 1405a5b90 | Client-only cache; no server equivalent needed |
| SpellTarget_ResolveAndValidateWrapper | 140398800 | Server validates wrappers via GameTable; no change needed |
| SpellTarget_ValidateTargetRelationship | 1403b44b0 | Dead/faction checks in server spell validation; no change needed |
| Channel data fInitialDelay/fMaxTime | 1405ed640 | Server uses Spell4Entry.CastTime; consistent |


---

## SpellTarget_ValidateWrapperAndTargets (FUN_1403ace00 @ 1403ace00)

**Called by:** SpellTarget_ResolveAndValidateWrapper as the final validation gate.

### Summary

This is the gatekeeper that confirms the resolved spell wrapper allows the chosen
target. It re-runs SpellService_ResolveSpellWrapper, reads two ValidTargets IDs
from the inner spell-wrapper struct, and calls a virtual ValidTargets-check
dispatch for both the primary and the secondary target.

### Entry Guard

FUN_1403ae8c0(DAT_140c65b70) is called first. If its return value is neither
 nor x13d, the function returns that value immediately without further
work. Only when the pre-condition state is OK (0) or the conditional-state (0x13d)
does validation proceed.

### Pseudocode

`c
ulonglong SpellTarget_ValidateWrapperAndTargets(
    undefined8 param_1,     // context
    undefined4 *param_2,    // spell_result array (param_2[0]=spell4_id, param_2[7]=target_id, param_2[9]=secondary_id)
    undefined8 param_3,
    undefined8 *param_4,    // failure-code output list (nullable)
    undefined8 param_5,
    int param_6) {

    uVar1 = DAT_140c65b70;              // global spell service obj
    pre_state = FUN_1403ae8c0(uVar1);   // get current pre-condition state

    if (pre_state != 0 && pre_state != 0x13d)
        return pre_state;               // early pass-through if unknown state

    if (param_6 != 0 && pre_state == 0x13d &&
        (param_4 == null || param_4[0x14] == 0))
        return 0x13d;                   // conditional-state early exit

    // resolve entities
    lVar4 = FUN_1403d90d0(DAT_140c65898, param_2[7]);   // primary entity
    lVar5 = FUN_1403d90d0(DAT_140c65898, param_2[9]);   // secondary entity

    if (lVar4 == 0) return 0x1e;  // primary entity not found

    // re-resolve wrapper (entity-level BST or global hash)
    lVar6 = SpellService_ResolveSpellWrapper(uVar1, *param_2, lVar4);
    if (lVar6 == 0) return 4;     // no wrapper

    inner = *(lVar6 + 0x70);      // inner spell-info struct

    valid_primary   = *(int *)(inner + 0x168);   // ValidTargets primary ID (0 = any)
    valid_secondary = *(int *)(inner + 0x16c);   // ValidTargets secondary ID

    prop_flags_byte = *(byte *)(inner + 0x108);  // SpellPropertyFlags low byte
    aoe_data_ptr    = *(lVar6 + 0x40);           // AoE data pointer

    primary_ok = (valid_primary == 0)            // 0 = no restriction
        || (aoe_data_ptr != 0 && (prop_flags_byte & 0x02))  // AoE + PropertyFlags bit 1
        || (*DAT_140c659a0_vtable_0x18(DAT_140c659a0, lVar4, valid_primary, lVar5, 0, 0) != 0);

    if (!primary_ok) {
        record_failure_code(param_4, 0x97);  // primary target invalid
        return 0x97;
    }

    if (valid_secondary != 0 && lVar5 != 0 &&
        *DAT_140c659a0_vtable_0x18(DAT_140c659a0, lVar5, valid_secondary, lVar4, 0, 0) == 0) {
        record_failure_code(param_4, 0x119);  // secondary target invalid
        return 0x119;
    }

    return pre_state;  // 0 or 0x13d = success
}
`

### Spell Wrapper Inner Struct (spell_wrapper + 0x70) - New Fields

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| +0x108 | byte | SpellPropertyFlags_low | Low byte of SpellPropertyFlags; bit 1 = AoE property |
| +0x168 | int32 | ValidTargets_primary_id | Primary ValidTargets row ID; 0 = no restriction |
| +0x16c | int32 | ValidTargets_secondary_id | Secondary ValidTargets row ID; checked only if secondary entity present |

---

## Spell4 and Spell4Base Table Field Offsets

Discovered from `Spell4Manager_BuildRuntimeData @ 14055e180`.
The function iterates ALL Spell4 rows and ALL Spell4Visual rows, validates every
cross-reference ID, and builds runtime lookup structures in a large manager object
(`param_1`). This is the authoritative source of Spell4 and Spell4Base field offsets
derived from client-code binary analysis.

### Spell4 Row Struct Offsets

Ghidra types `puVar28` as `ulonglong*` pointing to a Spell4 row. The 4-byte ID
fields are read as `(int)puVar28[N]` = low 32 bits at byte offset `N*8`.

| Byte offset | Ghidra accessor | Field name | Notes |
|------------|-----------------|------------|-------|
| `+0x10` | `(int)puVar28[2]` | `Spell4BaseId` | FK ? Spell4Base table |
| `+0xb8` | `(int)puVar28[0x2d]` | `StackGroupId` | FK ? Spell4StackGroup table; 0 = no stack group |
| `+0x114` | `*(int*)(puVar28+0x114)` | `AoeTargetConstraintsId` | FK ? Spell4AoeTargetConstraints; 0 = none |
| `+0x12c` | `*(int*)(puVar28+0x12c)` | `SpellCoolDownId[0]` | First cooldown slot |
| `+0x130` | `*(int*)(puVar28+0x130)` | `SpellCoolDownId[1]` | Second cooldown slot |
| `+0x134` | `*(int*)(puVar28+0x134)` | `SpellCoolDownId[2]` | Third cooldown slot |

### Spell4Base Row Struct Offsets

Ghidra types `lVar26` as `longlong` pointing to a Spell4Base row (GetById via
`&PTR_u_Spell4Base_140a6d118` using the Spell4BaseId from the Spell4 row).

| Byte offset | Ghidra accessor | Field name | Notes |
|------------|-----------------|------------|-------|
| `+0x08` | `*(int*)(lVar26+8)` | `HitResultsId` | FK ? Spell4HitResults |
| `+0x0c` | `*(int*)(lVar26+0xc)` | `TargetMechanicsId` | FK ? Spell4TargetMechanics |
| `+0x10` | `*(int*)(lVar26+0x10)` | `TargetAngleId` | FK ? Spell4TargetAngle |
| `+0x14` | `*(int*)(lVar26+0x14)` | `PrerequisitesId` | FK ? Spell4Prerequisites; 0 = none |
| `+0x18` | `*(int*)(lVar26+0x18)` | `ValidTargetsId` | FK ? Spell4ValidTargets |
| `+0x1c` | `*(int*)(lVar26+0x1c)` | `TargetGroupId` | FK ? TargetGroup (primary); 0 = none |
| `+0x34` | `*(int*)(lVar26+0x34)` | `TargetGroupId2` | FK ? TargetGroup (secondary/alternate); 0 = none |
| `+0x50` | `*(int*)(lVar26+0x50)` | `TargetCategory` | Controls which runtime collection spell goes into; values 1 and 3 have distinct handling |

### Universal ClientDB Hot-Swap Function Pointers (Confirmed from 14055e180)

These three globals are confirmed as universal DB hot-swap pointers used by ALL ClientDB tables -
NOT specific to Spell4StackGroup as previously labeled. The DB-specific pointer is passed
as the first argument.

| Global address | Role | Example call |
|---------------|------|------|
| `DAT_140c63838` | `ClientDB_GetAllRows(db_ptr)` | `(*DAT_140c63838)(&PTR_u_Spell4_140a6d0a8)` |
| `DAT_140c63840` | `ClientDB_GetById(db_ptr, id, hotswap_ctx)` | `(*DAT_140c63840)(&PTR_u_Spell4Base_140a6d118, id, DAT_140c63858)` |
| `DAT_140c63848` | `ClientDB_GetByIndex(db_ptr, index, hotswap_ctx)` | `(*DAT_140c63848)(&PTR_u_Spell4_140a6d0a8, i, DAT_140c63858)` |
| `DAT_140c63858` | hot-swap context (3rd arg to GetById/GetByIndex) | passed through |

### ClientDB Descriptor (PTR_u_*) Addresses - Newly Confirmed

The function accesses the following DB descriptor pointer addresses (PTR_u_* labels
assigned by Ghidra from nearby strings):

| Descriptor address | DB name |
|-------------------|---------|
| `140a6d0a8` | Spell4 |
| `140a6d118` | Spell4Base |
| `140a6d658` | Spell4Visual |
| `140a6dff8` | VisualEffect |
| `140a6d6c8` | SpellCoolDown |
| `140a6d930` | TargetGroup |
| `140a6d540` | Spell4TargetMechanics |
| `140a6d508` | Spell4TargetAngle |
| `140a6d620` | Spell4ValidTargets |
| `140a6d310` | Spell4HitResults |
| `140a6d380` | Spell4Prerequisites |
| `140a6d0e0` | Spell4AoeTargetConstraints |
| `140a6d498` | Spell4StackGroup (previously confirmed) |

---

## ValidTargetsCriteria_Evaluate - Full Criteria Type Decode

Decoded from `ValidTargetsCriteria_Evaluate @ 1403b4a20`.

`param_1` = CriteriaProxy pointer; `param_2` = TargetGroup row (int array).

### TargetGroup Row Layout

| Field index | Byte offset | Name | Notes |
|------------|------------|------|-------|
| `row[0]` | +0x00 | `id` | Row ID |
| `row[1]` | +0x04 | `(unknown)` | Not directly used in this function |
| `row[2]` | +0x08 | `criteriaType` | Switch discriminator (1-13 confirmed) |
| `row[3..9]` | +0x0c..+0x24 | `values[0..6]` | 7 int32 value slots for the type |

### Criteria Type ? Semantic

| Type | Odd/Even | CriteriaProxy vtable slot | Semantic |
|------|----------|--------------------------|----------|
| 1 | must-include | `+0x10` FactionGroupId | entity FactionGroupId MUST be in values[0..6] |
| 2 | must-exclude | `+0x10` FactionGroupId | entity FactionGroupId MUST NOT be in values[0..6] |
| 3 | must-include | `+0x30` Attr2_Unk118 | entity Attr2_Unk118 MUST be in values[0..6] |
| 4 | must-exclude | `+0x30` Attr2_Unk118 | entity Attr2_Unk118 MUST NOT be in values[0..6] |
| 5 | must-include | `+0x20` RaceId | entity RaceId MUST be in values[0..6] |
| 6 | must-exclude | `+0x20` RaceId | entity RaceId MUST NOT be in values[0..6] |
| 7 | must-include | `+0x28` ClassId | entity ClassId MUST be in values[0..6] |
| 8 | must-exclude | `+0x28` ClassId | entity ClassId MUST NOT be in values[0..6] |
| 9 | special | `+0x18` Field0x140_ListMatch | passes entire `values[0..6]` array; checks entity+0x140 list |
| 10 | all-pass | `+0x08` GetSubCriteria | all non-zero entries must recursively pass |
| 11 | all-fail | `+0x08` GetSubCriteria | all non-zero entries must recursively fail |
| 12 | must-include | `+0x38` UnitRaceId | entity UnitRaceId (via entity+0xd0 ptr) MUST be in values[0..6] |
| 13 | must-exclude | `+0x38` UnitRaceId | entity UnitRaceId MUST NOT be in values[0..6] |

Return codes:
- `0` = pass (target valid / no restriction triggered)
- `0x59` = fail (target invalid, excluded, or restriction triggered)
- `0x13d` = conditional pass (neutral/ignored state from recursive calls)

### CriteriaProxy Vtable Slot Usage (from ValidTargetsCriteria_Evaluate)

| Vtable byte offset | Cases | Role | Confirmed label |
|-------------------|-------|------|----------------|
| `+0x08` | 10, 11 | `GetSubCriteria()` ? sub-criteria row for recursive eval | `EntityCriteria_GetRelatedCriteriaThunk @ 1403b4a10` |
| `+0x10` | 1, 2 | getter: FactionGroupId | `EntityCriteria_GetFactionGroupId @ 1403b4940` (entity+0x18) |
| `+0x18` | 9 | `MultiCheck(values, count)` ? array-based eval (CheckType9) | `EntityCriteria_GetField0x140_ListMatch @ 1403b4960` (entity+0x140) |
| `+0x20` | 5, 6 | getter: RaceId | `EntityCriteria_GetRaceId @ 1403b49a0` (entity+0xd8) |
| `+0x28` | 7, 8 | getter: ClassId | `EntityCriteria_GetClassId @ 1403b49b0` (entity+0xdc) |
| `+0x30` | 3, 4 | getter: Attr2_Unk118 | `EntityCriteria_GetAttr2_Unk118 @ 1403b49c0` (entity+0x118) |
| `+0x38` | 12, 13 | getter: UnitRaceId | `EntityCriteria_GetUnitRaceId @ 1403b49e0` (entity+0xd0 ptr) |

---

## Entity Struct - Faction/Targeting Fields

Decoded from `Entity_GetFactionRelationship @ 14046c580`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x18` | pointer | `factionData` | Sub-struct pointer; faction rules live here |
| `+0x80` | int32 | `entityType` | 0x14 = special NPC type with override faction path |
| `*(factionData + 0x6c)` | int32 | `factionLockFlag` | Non-zero ? suppress normal faction lookup |
| `*(factionData + 0x148)` | int32 | `factionLockFlag2` | Non-zero ? suppress normal faction lookup |
| `*(factionData + 0xc0)` | pointer | `factionRelTable` | Pointer to 4-entry int32 array of faction relationship IDs |

The function iterates the 4-entry `factionRelTable`, calling a ValidTargets check for each. The
first entry whose check passes (or whose ID is zero) is returned. The default fallback is
`GameTable[0x19e]` field[+4].

For `entityType == 0x14`:
- `param_2 + 0xdc` = faction entry ID field in a context object
- `FUN_1401f31e0` = Faction GetById
- The relationship is at `faction_entry + 0x54`


| Descriptor address | DB name |
|-------------------|---------|
| `140a6d0a8` | Spell4 |
| `140a6d118` | Spell4Base |
| `140a6d658` | Spell4Visual |
| `140a6dff8` | VisualEffect |
| `140a6d6c8` | SpellCoolDown |
| `140a6d930` | TargetGroup |
| `140a6d540` | Spell4TargetMechanics |
| `140a6d508` | Spell4TargetAngle |
| `140a6d620` | Spell4ValidTargets |
| `140a6d310` | Spell4HitResults |
| `140a6d380` | Spell4Prerequisites |
| `140a6d0e0` | Spell4AoeTargetConstraints |
| `140a6d498` | Spell4StackGroup (previously confirmed) |

| +0x108 | byte | PropertyFlags low byte | Bit 1 (0x02): combined with AoE data bypasses primary ValidTargets |
| +0x168 | int | ValidTargets primary ID | 0 = any target accepted; runtime ID into Spell4ValidTargets table |
| +0x16c | int | ValidTargets secondary ID | Secondary target ValidTargets ID; 0 = no secondary check |

These complement the fields decoded in prior sessions (+0x40=AoE data ptr,
+0x50=channel data, +0x70=inner info struct).

### ValidTargets Dispatch

The ValidTargets check calls a virtual at *DAT_140c659a0 + 0x18:
`
(*(*DAT_140c659a0 + 0x18))(DAT_140c659a0, entity, validtargets_id, other_entity, 0, 0)
`
DAT_140c659a0 is a separate service object (not the same as DAT_140c65b70
spell service). The vtable offset x18 (slot 3) dispatches to the actual
ValidTargets lookup. This is not yet decoded; likely SpellService_CheckValidTargets.

### Error Code Table

| Code | Hex | Meaning |
|------|-----|---------|
| 0 | 0x00 | Success |
| 4 | 0x04 | No spell wrapper resolved |
| 30 | 0x1e | Primary entity not found in world |
| 151 | 0x97 | Primary target fails ValidTargets check |
| 281 | 0x119 | Secondary target fails ValidTargets check |
| 317 | 0x13d | Pre-condition pass-through (conditional state) |

### PropertyFlags Bit 1 - AoE ValidTargets Override

When spell_wrapper + 0x40 (AoE data pointer) is non-null **and**
spell_wrapper + 0x70 + 0x108 bit 1 (0x02) is set, the primary ValidTargets
check always passes. This is consistent with AoE spells that hit all entities
in radius regardless of normal ValidTargets restrictions.

Current SpellPropertyFlags in the server does not include bit 0x02. This bit
is a note for future investigation - it may be relevant for spells that should
bypass target restrictions when in AoE mode.

### Server Implementation Status

| Concern | Status |
|---------|--------|
| ValidTargets primary check | Server CheckPrimaryTargetValidMask uses Spell4ValidTargets.TargetBitmask - functionally equivalent to client alid_primary_id |
| ValidTargets secondary check | Server has no secondary ValidTargets path currently; secondary target used for e.g. buff transfers |
| PropertyFlags bit 1 AoE bypass | Not in server; noted for future when AoE spells show unexpected target rejection |
| Error codes 0x97/0x119 | Client-side only; server has its own CastResult enum |
| No server changes needed this pass | ? |

---

## SpellWrapper (Resolved) Object Layout

Decoded from `Lua_GameSpell_GetId @ 1405e95f0`, `GetBaseSpellId @ 1405e96a0`,
`GetTier @ 1405e9840`, `GetChannelData`, `SpellTarget_ValidateWrapperAndTargets`,
`SpellCast_ValidateAndDispatch`.

`SpellService_ResolveSpellWrapper(spellServiceGlobal, spellId, entityContext)` ? `lVar`.

### SpellWrapper Top-level Fields

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x40` | pointer | `aoeDataPtr` | Non-null for AoE spells; used in ValidTargets bypass check |
| `+0x50` | pointer | `channelDataPtr` | Non-null for channeled spells |
| `+0x70` | pointer | `innerDataPtr` | Pointer to inner spell data struct |

### SpellWrapper InnerData (at `*(wrapper + 0x70)`)

| Byte offset | Type | Name | Lua accessor / Notes |
|------------|------|------|----------------------|
| `+0x00` | int32 | `Spell4Id` | `GetId` |
| `+0x04` | int32 | `Spell4BaseId` | `GetBaseSpellId` |
| `+0x08` | byte | `tier` | `GetTier` |
| `+0x18` | int32 | `castMethod` | `GetCastMethod`; 3 = no-target mode; 7 = freeform/chain composite |
| `+0x70` | float | `tierCountFloat` | Range calc: `fVar7 = *(float *)(inner + 0x70) - 1.0` |
| `+0x78` | float | `targetAngle` | `GetTargetAngle` (radians) |
| `+0x7c` | int32 | `targetingType` | Target type code (see table below) |
| `+0x80` | int32 | `overrideSpellId` | Redirect/linked spell ID (0 = none) |
| `+0x9c` | int32 | `targetSubType` | Alternate target qualifier; non-zero overrides IsSelfSpell check |
| `+0xf4` | int32 | `damageSchool` | `GetSchool`; damage school enum |
| `+0xf8` | int32 | `primaryEffectType` | Primary spell effect type number (e.g., 0xe, 0x24) |
| `+0xfc` | int32 | `spellSlotType` | ActionSet slot type: 5 = ability (slots 0-7), non-5 = passive/AMP |
| `+0x108` | uint32 | `propertyFlags` | Bit field - see PropertyFlags table below |
| `+0x10c` | uint32 | `beneficialFlags` | Bit field - see BeneficialFlags table below |
| `+0x128` | int32 | `validationBypassFlag` | Non-zero: skip relationship check for primary target |
| `+0x154` | uint32 | `spellFlagsWord` | Bit 0x400: override/substitute enabled; Bit 0x200: item use mode |
| `+0x168` | int32 | `validTargetsPrimaryId` | ValidTargets ID for caster check (0 = none) |
| `+0x16c` | int32 | `validTargetsSecondaryId` | ValidTargets ID for target check (0 = none) |
| `+0x180` | int32 | `substituteSpellId` | Substitute spell (when flags word bit 0x400 set) |
| `+0x184` | uint32 | `thresholdTimeMs` | `GetThresholdTime`; threshold cast time in milliseconds |
| `+0x198` | int32 | `prerequisiteId` | Prerequisites ID (Spell4Prerequisites table); 0 = none |
| `+0x1b0` | int32 | `questInteractionFlag` | Non-zero: cast triggers QuestInteraction flow |
| `+0x1d0` | int32 | `tradeskillVendorFlag` | Non-zero: triggers tradeskill/vendor flow |
| `+0x1d8` | int32 | `additionalInteractionFlag` | Non-zero: used for NPC activation |

**PropertyFlags (`inner + 0x108`) bit map:**

| Bit | Mask | Name |
|-----|------|------|
| 1 | `0x00000002` | AoEValidTargetsBypass |
| 6 | `0x00000040` | IsMovingInterrupted (`IsMovingInterrupted` Lua getter) |
| 22 | `0x00400000` | IsFreeformTarget (`IsFreeformTarget` Lua getter) |
| 29 | `0x20000000` | ServiceTokenRequired |

**BeneficialFlags (`inner + 0x10c`) bit map:**

| Bit | Mask | Name |
|-----|------|------|
| 26 | `0x04000000` | IsBeneficial (`IsBeneficial` Lua getter) |
| 28 | `0x10000000` | IsGroundTargeted (bypass check in SpellTarget_ResolveTargetEntity) |

### ChannelData Struct (at `*(wrapper + 0x50)`)

| Byte offset | Type | Name | Lua key |
|------------|------|------|---------|
| `+0x00` | uint32 | `initialDelayMs` | "fInitialDelay" (milliseconds) |
| `+0x04` | uint32 | `maxTimeMs` | "fMaxTime" (milliseconds) |

### TargetingType Codes (from SpellTarget_ResolveTargetEntity and Game_Spell_IsSelfSpellDelegate)

| TargetingType value | Behavior | Entity field used | IsSelfSpell |
|--------------------|----------|------------------|-------------|
| 0 | Self-target | `entity + 0x8` (self entity ID) | Yes (0x85 bit 0) |
| 1 | Cursor/selected target | `entity + 0x108` (cursor target ID) | No |
| 2 | Self-target | `entity + 0x8` | Yes (0x85 bit 2) |
| 3 | Cursor/selected target | `entity + 0x108` | No |
| 4 | Item targeting | `entity + 0x6ce8..0x6d00` (item-use spell context) | No |
| 5 | Cursor/selected target | `entity + 0x108` | No |
| 6 | Self-target | `entity + 0x8` | No (0x85 bit 6 unset - self-ID but not "self spell") |
| 7 | Self-target | `entity + 0x8` | Yes (0x85 bit 7) |
| 8 | Cursor/selected target | `entity + 0x108` | No |
| other | No target | 0 | No |
| `inner+0x18 == 3` | No target override | 0 (overrides targetingType) | No |

`IsSelfSpell` bitmask: `0x85 = 0b10000101` ? bits 0, 2, 7 set = types {0, 2, 7} are "self" type.
`Game_Spell_IsSelfSpellDelegate @ ?`: reads `inner + 0x7c`, uses bitmask 0x85; also checks `inner+0x9c`.

Cursor-target bitmask: `0x12a = 0b100101010` (bits 1,3,5,8 set) = types that auto-select nearest when no cursor target set (within 4.0 units, uses hitbox radii).

---

## Entity Struct - Spell Cast / Action Slots

Decoded from `SpellCast_ValidateAndDispatch`, `SpellCast_SendClientSpellCastState`,
`SpellCast_SendClientCastSpellOrPosition`, `SpellCast_ResolveTargetsAndValidate`,
`SpellTarget_ResolveTargetEntity`, `Entity_LookupSpellWrapperInBST`.

### Entity Core Spell Fields

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x08` | int32 | `entityId` | Own entity ID (confirmed many times) |
| `+0x18` | pointer | `factionData` | Faction rules sub-object |
| `+0x78` | pointer | `activeSpellContext` | Points to current active spell; `*(context+0x98)` = Spell4Id |
| `+0x80` | int32 | `entityType` | 0x14 = special NPC type |
| `+0xa0` | struct | `actionSetStorage` | ActionSet/HotkeySet container |
| `+0xa90` | pointer | `actionBarSlotArray` | Pointer to array of hotkey slot pointers |
| `+0xa98` | uint64 | `actionBarSlotCount` | Number of action bar slots |
| `+0x108` | int32 | `cursorTargetId` | Current cursor/mouse-over target entity ID |
| `+0x11e0` | float | `positionX` | World position X |
| `+0x11e4` | float | `positionY` | World position Y |
| `+0x11e8` | float | `positionZ` | World position Z |
| `+0x540` | hash map | `spellWrapperByIdMap` | Spell4 id -> spell wrapper; `SpellService_LookupSpellWrapperById` |
| `+0x6364` | int32 | `currentSpellTrackingId` | Set when slot index == 2 |
| `+0x6490` | pointer | `castContext` | Follow-up/chain cast context sub-struct |
| `+0x6648` | int32 | `spellCastState` | Value 7 = specific in-progress state |
| `+0x6658` | struct | `castSubContext` | Cast phase sub-context |
| `+0x6718` | pointer | `globalEntityContext` | Context pointer checked in multicast scenario |
| `+0x67b0` | int32 | `pendingSpellCheckId` | Pending spell for cooldown validation |
| `+0x6ce8` | pointer | `itemUseSpellWrapper` | Item-use spell context, field 1 |
| `+0x6cf0` | uint64 | `itemUseSpellData1` | Item-use spell context, field 2 |
| `+0x6cf8` | uint64 | `itemUseSpellData2` | Item-use spell context, field 3 |
| `+0x6d00` | uint64 | `itemUseSpellData3` | Item-use spell context, field 4 (low 32 = 9 for TargetingType 4) |
| `+0x6d10` | struct | `targetingContext` | Targeting context used by SpellCast_ResolveTargetsAndValidate |
| `+0x6d90` | pointer | `spellTranslateService` | Spell ID translation service (wraps spell objects) |
| `+0x7040` | pointer | `unkFlag7040` | Non-null triggers FUN_14057a2c0 at start of cast |
| `+0x7340` | pointer | `luaEventContext` | Used to dispatch named Lua events (SpellCastFailed etc.) |
| `+0x7b4c` | int32 | `effectDispatchStatus` | Written during spell effect dispatch |
| `+0x7ba0` | byte | `groundTargetFlags` | Bit 0: in ground-targeted mode |
| `+0x7d18` | BST root | `spellWrapperBST` | BST of SpellId ? SpellWrapper; `Entity_LookupSpellWrapperInBST` |

### Entity CastContext Sub-struct (at `*(entity + 0x6490)`)

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x108` | int32 | `chainCastSpellId` | SpellId of chain/follow-up spell |
| `+0x1cc` | int32 | `channelInterruptFlag` | Non-zero: interrupt current channel first |
| `+0x1070` bit 0x200 | flag | `someCastMode` | Combined with AoE data check |

### SpellWrapper BST Node Layout (at entity+0x7d18)

BST sorted by SpellId. Each node:
| Byte offset | Type | Name |
|------------|------|------|
| `+0x10` | pointer | left child |
| `+0x18` | pointer | right child |
| `+0x20` | uint32 | SpellId (key) |
| `+0x28` | pointer | SpellWrapper (value) |

### ActionBar Slot Object (from `SpellCast_SendClientCastSpellOrPosition`)

- `*(param_1 + 0xa90)` = pointer to array; indexed by slot number (0-based)
- Each entry is a pointer to a slot object with vtable
- Slot vtable `+0x70` = validation function (returns 0x1c = locked/disabled)

### SpellCast Return Codes (Extended)

| Code | Hex | Meaning |
|------|-----|---------|
| 0 | 0x00 | Success |
| 4 | 0x04 | No spell resolved / slot has no spell |
| 17 | 0x11 | Prerequisite failed |
| 22 | 0x16 | No ActionSet slot for this cast |
| 23 | 0x17 | No valid NPC interaction |
| 30 | 0x1e | No active target (entity+0x78 = null) |
| 105 | 0x69 | Out of range (triggers deferred approach) |
| 151 | 0x97 | Caster fails ValidTargets check |
| 281 | 0x119 | Secondary target fails ValidTargets check |
| 279 | 0x117 | GiveItem range check failed |
| 317 | 0x13d | Conditional pass-through |
| 0x11d | 285 | Action slot locked/disabled |

---

## SpellService Globals

| Address | Name | Role |
|---------|------|------|
| `DAT_140c65b70` | `SpellServiceGlobal` | Main spell service; passed to `SpellService_ResolveSpellWrapper` |
| `DAT_140c65898` | `LocalPlayerEntityGlobal` | Global ptr to local player entity |
| `DAT_140c659a0` | `ValidTargetsServiceGlobal` | Target validation service; vtable slot `+0x18` = check fn |
| `DAT_140c65b70` | `SpellServiceGlobal` | Also passed to ResolveTargetFlags, GetMaximumRange, GetMinimumRange |

---

## Auto-Target Logic (SpellTarget_ResolveTargetEntity)

When `targetingType` is cursor-based (types 1,3,5,8) AND `param_5 == 0` (no cursor target) AND `param_4 == 0`:
1. Load `GameTable[0x145]` field `+0x18` = float range threshold (default 4.0)
2. Call `FUN_14055a5f0` = `FindNearestTargetable(entity, 1, 1, ...)` ? returns entityId
3. If entity found AND distance < (range + both hitbox radii): `TargetSelection_ApplySelectionAndDispatch(entity, nearbyId)`
4. Use the nearby entity as the target


---

## ValidTargetsCriteria_Evaluate, Entity_GetFactionRelationship, SpellTarget_LogValidationMask

### ValidTargetsCriteria_Evaluate (FUN_1403b4a20 @ 1403b4a20)

**Called by:** vtable dispatch from SpellTarget_ValidateTargetRelationship when 	arget_mask is non-zero.

This is the heart of the client-side Spell4ValidTargets evaluation. It takes
an entity and a criteria descriptor struct, dispatches on the checkType field,
and tests the entity's attribute against up to 7 allowed/excluded values.

#### Criteria Struct Layout

`
struct ValidTargetsCriteria {
    int id;           // [0] = criteria row ID (used in cases 10/11 as "current" to skip self)
    int unk1;         // [1] = unused in this function
    int checkType;    // [2] = dispatch key (1-13)
    int values[7];    // [3..9] = up to 7 int IDs; 0 = empty slot
};
`

#### Check Types

| checkType | Entity Vtable | Semantics |
|-----------|---------------|-----------|
| 1 | +0x10 | Must match one of values[] |
| 2 | +0x10 | Must NOT match any of values[] |
| 3 | +0x30 | Must match one of values[] |
| 4 | +0x30 | Must NOT match any of values[] |
| 5 | +0x20 | Must match one of values[] |
| 6 | +0x20 | Must NOT match any of values[] |
| 7 | +0x28 | Must match one of values[] |
| 8 | +0x28 | Must NOT match any of values[] |
| 9 | +0x18 | Recursive sub-criteria (passes values[] as sub-array of 7) |
| 10 | +0x08 ? recurse | At least one related-entity passes recursive check |
| 11 | +0x08 ? recurse | All related-entities fail recursive check |
| 12 (0xc) | +0x38 | Must match one of values[] |
| 13 (0xd) | +0x38 | Must NOT match any of values[] |

Entity vtable slots queried (attribute getters):
- +0x08: get related/owner entity pointer
- +0x10: attribute A (faction class? faction group?)
- +0x18: recursive criteria dispatcher
- +0x20: attribute C
- +0x28: attribute D
- +0x30: attribute B
- +0x38: attribute E

Return codes:  = pass, x59 = fail, x13d = conditional pass.

#### Pseudocode (abbreviated)

`c
ulonglong ValidTargetsCriteria_Evaluate(entity *param_1, int *param_2) {
    if (param_2 == null) return 0x59;

    int *values = param_2 + 3;  // values[0..6]

    switch (param_2[2]) {  // checkType
    case 1:  // must-match attribute A
        attr = (*vtable_0x10)(param_1);
        for (i=0; i<7; i++) if (values[i] && attr==values[i]) return 0;  // pass
        return 0x59;  // no match = fail
    case 2:  // must-not-match attribute A
        attr = (*vtable_0x10)(param_1);
        for (i=0; i<7; i++) if (values[i] && attr==values[i]) return 0x59;  // match = fail
        return 0;  // no match = pass
    // ... cases 3-8: same pattern with vtable +0x30, +0x20, +0x28, +0x38
    case 9:  // recursive sub-criteria
        return (*vtable_0x18)(param_1, values, 7);
    case 10:  // any related-entity passes
        for (i=0; i<7; i++) {
            if (values[i] && param_2[0]!=values[i]) {
                related = (*vtable_0x08)(param_1);
                result = ValidTargetsCriteria_Evaluate(param_1, related);
                if (result==0 || result==0x13d) return 0;  // pass if any match
            }
        }
        return 0x59;
    case 11:  // no related-entity passes (inverse of 10)
        ...
    }
}
`

#### Server Implication

The server uses Spell4ValidTargets.TargetBitmask as a simple bitfield for
target checks. The client has a richer multi-criteria system with 13 check types
and per-row value arrays. The server's ValidTargetObjectMask=0x02 and
ValidTargetDeadMask=0x08 are a simplified projection of this system.

**No server changes required** - the server's bitfield approach is functionally
correct for currently implemented spells.

---

### Entity_GetFactionRelationship (FUN_14046c580 @ 14046c580)

**Called by:** SpellTarget_ValidateTargetRelationship to read the relationship
group of param_1 relative to caster context param_2.

#### Logic Summary

`c
undefined4 Entity_GetFactionRelationship(entity *param_1, context *param_2) {
    // Default result from GameTable row 0x19e (Faction table ID 414)
    row = FUN_140200220(0x19e);
    result = (row != 0) ? *(int *)(row + 4) : 0;

    // Special path: entity type 0x14
    if (*(int *)(param_1 + 0x80) == 0x14) {
        faction_entry = FUN_1401f31e0(*(int *)(param_2 + 0xdc));
        if (faction_entry != 0 &&
            FUN_14045a950(param_1, *(int *)(param_2 + 8)) != 0)
            return *(int *)(faction_entry + 0x54);  // override faction
    } else {
        // Default path: faction-override array at entity+0x18+0xc0
        state = *(longlong *)(param_1 + 0x18);
        if (state != 0 &&
            *(int *)(state + 0x6c) == 0 &&    // flag A cleared
            *(int *)(state + 0x148) == 0) {   // flag B cleared
            array = *(longlong *)(state + 0xc0);  // faction-override entries (max 4)
            for (i=0; i<4; i++) {
                id = *(int *)(array + 0x10 + i*4);
                if (id == 0 || ValidTargets_Check(param_2, id, param_1)) {
                    return *(int *)(array + i*4);  // return matching override
                }
            }
        }
    }
    return result;
}
`

#### New Entity/State Struct Fields

| Expression | Meaning |
|-----------|---------|
| ntity + 0x80 | Entity type int (0x14 = special type) |
| ntity + 0x18 | State/component struct pointer |
| ntity+0x18 + 0x6c | Flag A (must be 0 for faction override to apply) |
| ntity+0x18 + 0x148 | Flag B (must be 0 for faction override to apply) |
| ntity+0x18 + 0xc0 | Pointer to faction-override entry array (up to 4 entries) |
| context + 0xdc | Faction entry ID in context |
| context + 0x08 | Caster membership ID for FUN_14045a950 check |

#### Server Implication

Server faction checking uses IWorldEntity relationship queries and doesn't model
the client's faction-override array or entity-type-0x14 special path. These are
client presentation details. **No server change needed.**

---

### SpellTarget_LogValidationMask (FUN_1403b4a10 @ 1403b4a10)

`c
void SpellTarget_LogValidationMask(undefined8 param_1, undefined4 param_2) {
    FUN_140240b40(param_2);  // diagnostic/tracing call
}
`

Thin oid wrapper - passes param_2 (target_mask uint) to FUN_140240b40.
Has no return value and cannot affect validation results. Likely a diagnostics
or client-side logging call invoked via vtable when target validation fails.
FUN_140240b40 not yet decoded; low priority (tracing code).

---

### SpellService_LookupSpellWrapperById (1407a0fd0) - Already Documented

Confirmed from this session: the function was already labeled in the Ghidra
project from a prior session and is present in unction_labels.csv. It
performs a chained hash table lookup at service_obj + 0x540 by spell4_id
and returns the stored spell wrapper pointer. All callers now use the proper name.

---

### Updated Function Name Cross-References

| Old FUN_ Reference | Proper Name | Used In |
|--------------------|------------|---------|
| FUN_1407a0fd0 | SpellService_LookupSpellWrapperById | SpellTarget_ResolveAndValidateWrapper label |
| FUN_1403b4a10 | SpellTarget_LogValidationMask | SpellTarget_ValidateTargetRelationship label |
| FUN_1403b4a20 | ValidTargetsCriteria_Evaluate | SpellTarget_ValidateTargetRelationship label |
| FUN_14046c580 | Entity_GetFactionRelationship | SpellTarget_ValidateTargetRelationship label |

---

### ValidTargetsCriteria_Evaluate - CriteriaProxy Vtable Full Decode

**Vtable at data address `140b66440` (concrete CriteriaProxy implementation).**

A second abstract vtable exists at `140b66400` sharing slots 0/1 but with `_purecall` for slots 2-7.

**CriteriaProxy struct layout (confirmed):**
```c
struct CriteriaProxy {
    void**  vtable;      // [+0x00] points to vtable data at 140b66440
    Entity* entity;      // [+0x08] entity pointer from SpellTarget_ValidateTargetRelationship local_10
};
```

**Full vtable slot map:**

| Data Addr  | Vtable Slot | Function Addr | Label / Semantic |
|------------|-------------|---------------|-----------------|
| 140b66440  | [+0x00]     | 1403b4910     | `CriteriaProxy_VTable0` (destructor/RTTI, 24 refs, role unconfirmed) |
| 140b66448  | [+0x08]     | 1403b4a10     | `EntityCriteria_GetRelatedCriteriaThunk` - `MOV ECX,EDX; JMP Spell4ValidTargets_GetCriteriaById` |
| 140b66450  | [+0x10]     | 1403b4940     | `EntityCriteria_GetFactionGroupId` - `[entity+0x18]+0x00` |
| 140b66458  | [+0x18]     | 1403b4960     | `EntityCriteria_GetCreature2Id_ListMatch` - reads entity+0x140 (Creature2Id) DWORD, list-match (checkType 9) |
| 140b66460  | [+0x20]     | 1403b49a0     | `EntityCriteria_GetRaceId` - `entity+0xd8` |
| 140b66468  | [+0x28]     | 1403b49b0     | `EntityCriteria_GetClassId` - `entity+0xdc` |
| 140b66470  | [+0x30]     | 1403b49c0     | `EntityCriteria_GetFaction2Id` - `[entity+0x118]->vtable[+0x18]()` returns Faction2Id; entity+0x118 = Faction2 component |
| 140b66478  | [+0x38]     | 1403b49e0     | `EntityCriteria_GetUnitRaceId` - `*[entity+0xd0]` |
| 140b66480  | [+0x40]     | 1403b4a00     | `EntityCriteria_GetField0x140` - `entity+0x140` (role unconfirmed) |

**`ValidTargetsCriteria_Evaluate` checkType ? confirmed semantic:**

| checkType | Vtable Slot | Function | Semantic | Confirmation Source |
|-----------|-------------|----------|----------|---------------------|
| 1 (must-match)   | [+0x10] | 1403b4940 | **FactionGroupId** = `[entity+0x18+0x00]` | Entity_GetFactionRelationship; entity+0x18 = faction-state struct |
| 2 (must-not)     | [+0x10] | 1403b4940 | **FactionGroupId** (inverse) | same |
| 3 (must-match)   | [+0x30] | 1403b49c0 | **Faction2Id** = `[entity+0x118]->vtable[+0x18]()` | TargetGroup type=3/4 data value range cross-reference (all values 166-1112 are valid Faction2 IDs; min Faction2 ID = 164) |
| 4 (must-not)     | [+0x30] | 1403b49c0 | **Faction2Id** (inverse) | same |
| 5 (must-match)   | [+0x20] | 1403b49a0 | **RaceId** = `entity+0xd8` | Lua_GameUnit_GetRaceId @ 14064a080 |
| 6 (must-not)     | [+0x20] | 1403b49a0 | **RaceId** (inverse) | same |
| 7 (must-match)   | [+0x28] | 1403b49b0 | **ClassId** = `entity+0xdc` | Lua_GameUnit_GetClassId @ 14064a1a0 |
| 8 (must-not)     | [+0x28] | 1403b49b0 | **ClassId** (inverse) | same |
| 9                | [+0x18] | 1403b4960 | **Creature2Id list-match** (entity+0x140 vs up to 7 Creature2 IDs) - confirmed via TargetGroup type=9 data cross-reference | TargetGroup type=9 data values (2202, 2441, 3086, 4859-4861, etc.) all verified as valid Creature2 IDs |
| 10               | [+0x08] | 1403b4a10 | **Related-criteria lookup** (must-match, recursive) | thunk ? Spell4ValidTargets_GetCriteriaById |
| 11               | [+0x08] | 1403b4a10 | **Related-criteria lookup** (must-not, recursive) | same |
| 12 (0xc, must-match) | [+0x38] | 1403b49e0 | **UnitRaceId** = `*[entity+0xd0]` | Lua_GameUnit_GetUnitRaceId @ 14064a100 |
| 13 (0xd, must-not)  | [+0x38] | 1403b49e0 | **UnitRaceId** (inverse) | same |

**Forwarding thunk `1403b4a10` mechanics:**
- Assembly: `MOV ECX, EDX; JMP 0x140240b40`
- When called from checkTypes 10/11: caller passes CriteriaProxy in RCX, criteria-entry ID (from values array `*piVar6`) in RDX
- Thunk ignores proxy (RCX), copies ID to ECX, jumps to `Spell4ValidTargets_GetCriteriaById`
- Returns pointer to criteria struct for recursive `ValidTargetsCriteria_Evaluate` call
- **Previously mislabeled** as `SpellTarget_LogValidationMask`; corrected

**`Spell4ValidTargets_GetCriteriaById` (`140240b40`):**
- Takes a uint32 criteria ID, returns pointer to the criteria struct (int array)
- Called by the thunk for types 10/11; likely called elsewhere for top-level criteria resolution

**Entity field offsets confirmed from vtable decode + Lua_GameUnit cross-reference:**

| Offset | Type   | Meaning | Source |
|--------|--------|---------|--------|
| +0x18  | ptr    | Faction-state component (first int = FactionGroupId) | EntityCriteria_GetFactionGroupId + Entity_GetFactionRelationship |
| +0x24  | byte   | Dead flag (bit 0) | SpellTarget_ValidateTargetRelationship |
| +0x80  | int32  | Entity type (0x14 = special faction path) | Entity_GetFactionRelationship |
| +0xd0  | ptr    | UnitRace component (first int = UnitRaceId) | EntityCriteria_GetUnitRaceId + Lua_GameUnit_GetUnitRaceId |
| +0xd8  | int32  | RaceId | EntityCriteria_GetRaceId + Lua_GameUnit_GetRaceId |
| +0xdc  | int32  | ClassId | EntityCriteria_GetClassId + Lua_GameUnit_GetClassId |
| +0x118 | ptr    | Faction2 component; `vtable[+0x18]()` returns Faction2Id (int32). Returns 0 if null. checkType 3/4 filter. Evidence: all TargetGroup type=3/4 data values (166-1112) fall within the Faction2 table ID range (min=164). In NexusForever: IWorldEntity.Faction1/Faction2 are `Faction` enum values whose integers are Faction2 IDs (Dominion=166, Exile=167). | EntityCriteria_GetFaction2Id confirmed + TargetGroup cross-reference |
| +0x140 | int32  | Creature2Id (entity's template/prototype creature ID). checkType 9 list-match via vtable[+0x18] (1403b4960), simple getter via vtable[+0x40] (1403b4a00). Evidence: all TargetGroup type=9 data values (2202, 2441, 3086, 4859-4861, etc.) verified as valid Creature2 IDs. Players return 0. In NexusForever: IWorldEntity.CreatureId. | EntityCriteria_GetCreature2Id + TargetGroup cross-reference |

**Remaining unknowns:**
- `1403b4910` vtable slot 0 role (destructor/RTTI, 24 refs; role unconfirmed)

**Server implication - no code change needed.**
The server's `Spell4ValidTargets.TargetBitmask` (a simplified bitfield projection of the client's
criteria rows) correctly covers the known criteria semantics. The new confirmed mappings
(FactionGroupId, RaceId, ClassId, UnitRaceId) validate that the server's coarse bitmask approach
is sufficient and the criteria system does not require server-side criteria row evaluation.

---

### Updated Function Name Cross-References (addendum)

| Old FUN_ Reference | Proper Name | Notes |
|--------------------|------------|-------|
| FUN_140240b40 | Spell4ValidTargets_GetCriteriaById | Criteria ID?struct lookup; forwarding target of 1403b4a10 |
| FUN_1403b4940 | EntityCriteria_GetFactionGroupId | vtable[+0x10]; entity+0x18+0x00 |
| FUN_1403b4960 | EntityCriteria_GetCreature2Id_ListMatch | vtable[+0x18]; reads entity+0x140 (Creature2Id) DWORD, list-match; checkType 9 (NOT recursive sub-criteria) |
| FUN_1403b49a0 | EntityCriteria_GetRaceId | vtable[+0x20]; entity+0xd8 |
| FUN_1403b49b0 | EntityCriteria_GetClassId | vtable[+0x28]; entity+0xdc |
| FUN_1403b49c0 | EntityCriteria_GetFaction2Id | vtable[+0x30]; entity+0x118 = Faction2 component; vtable[+0x18]() returns Faction2Id |
| FUN_1403b49e0 | EntityCriteria_GetUnitRaceId | vtable[+0x38]; *[entity+0xd0] |
| FUN_1403b4a00 | EntityCriteria_GetCreature2Id | vtable[+0x40]; entity+0x140 = Creature2Id |
| FUN_1403b4a10 | EntityCriteria_GetRelatedCriteriaThunk | vtable[+0x08]; previously mislabeled SpellTarget_LogValidationMask |

## Recent mapped-function blocker audit (2026-05-19)

Recent target-validation and reward-rotation labels were rechecked against the
current NexusForever runtime surfaces to see whether any previously blocked work
is now safely implementable.

- `SpellTarget_ResolveTargetEntity` target types `5`, `6`, and `8` are already
  implemented in `CharacterSpell.ResolvePrimaryTargetId`: `0`/`2`/`6` resolve to
  the caster and `1`/`3`/`5`/`7`/`8` resolve through the current visible target.
  Type `7` is intentionally different from the client entity/anchoring switch:
  the client groups it with caster resolution for UI auto-fire behavior, while
  the server keeps it on the selected combat target for effect application.
- The type-`7` service tree at `service_obj+0x788` and wrapper hash map at
  `service_obj+0x540` are mapped as client wrapper/UI lookup structures. They do
  not expose a server-owned data source that would change target selection, and
  the current server current-target path for type `7` remains the safe behavior.
- `ValidTargetsCriteria_Evaluate` and the CriteriaProxy vtable are now fully decoded:
  faction-group, Faction2, race, class, unit-race, Creature2Id, nested criteria, and
  related-criteria checks are all identified. `entity+0x118` is the Faction2 component
  (vtable[+0x18]() returns Faction2Id; confirmed by TargetGroup type=3/4 data correlation -
  all values fall within Faction2 ID range, min=164). `entity+0x140` is the Creature2Id
  (confirmed by TargetGroup type=9 data correlation - values 2202, 2441, 3086, etc. are
  valid Creature2 IDs; in NexusForever: `IWorldEntity.CreatureId`). Full server-side criteria
  evaluation remains unnecessary: `Spell4ValidTargetsEntry` currently exposes only `Id` and
  `TargetBitmask`, and the existing `TargetBitmask` projection is the verified server path.
  No server code changes needed.
- Reward rotation network row shapes are implemented as writable packet models
  for opcodes `0x07C7` through `0x07CB`, but the manager remains blocked for
  grants and send-side state because no authoritative schedule source or
  verified delta-opcode semantic split has been recovered.
- Previously blocked account pending-item groups have already moved to runtime
  state with claim, return, and online gift routing. No additional recent
  decompile addition changes that boundary.
- Taxi unlock persistence, durable rune persistence, hot/cold discovery
  mutation, harvest node/material grants, and
  `UpdateSpellInProgress` still lack the required server-owned state model or
  receiver/transaction evidence. They remain blocked rather than guessed.
- `RavelSignal` receiver behavior is now implemented at the script-dispatch
  level: `HandleEffectRavelSignal` calls `target.SendSignal(signalId)`, which
  invokes `IWorldEntityScript.OnSignal(signalId)` on the target entity's script
  collection. The binary dispatch architecture is mapped in the section below.

### RavelSignal spell effect dispatch architecture

`SpellEffectType.RavelSignal = 0x51 = 81`. The WildStar client has two
spell-effect dispatch tables split at `DespawnUnit = 0x61 = 97`:

**High-range dispatch** (effectTypes `0x61`-`0x972`): `Entity_ExecuteSpellEffectHighRange`
at `0x1403ec6a0`, dispatch index = `effectType ? 0x61`, jump table at `0x1403f1344`
(raw int32 RVA offsets relative to `0x140000000` image base, readable directly from
the PE `.text` section at file offset `0x3F0744`).

Jump table statistics: 2322 total entries, **645 non-default**, default handler
`0x1403F12D2`. The dispatch function also guards on `entity+0x7928 != 0` (network
connection live) and `param_2 == entity+0x7928+0x98` (channel ID match) before dispatch;
stores a tick at `entity+0x7b4c`.

First non-default entries (effectType ? handler):

| effectType | Name (SpellEffectType.cs) | Handler |
|---|---|---|
| 0x061 | DespawnUnit | 0x1403EC9FF |
| 0x068 | UNUSED104 | 0x1403F0B50 |
| 0x070 | DisguiseOutfit | 0x1403F102F |
| 0x07E | SetMatchingEligibility | 0x1403EEF04 |
| 0x091 | UnitPropertyConversion | 0x1403EEB67 |
| 0x092 | ActivateSpellCooldown | 0x1403F103D |

Full table saved to `session-state/files/highrange-dispatch-table.csv`.

The high-range handler addresses in this table are embedded jump-table case
targets inside `Entity_ExecuteSpellEffectHighRange`, not independent Ghidra
function entries. Keep them in this finding/tracker context rather than in
`function_labels.csv`; the durable function label belongs on the containing
entry point at `0x1403ec6a0`.

**Low-range dispatch** (effectTypes `0x01`-`0x60`): Function at `0x1406b1300`,
dispatch index = `effectType ? 1`, two-level table:
```asm
1406b1503: lea  eax, [r13 ? 1]      ; effectType ? 1 = index
1406b1507: cmp  eax, 0x5f           ; bounds check (max index 95)
1406b150a: ja   0x1406b321e         ; out of range ? epilogue
1406b1510: lea  rdx, [rip ? 0x6b1517]  ; rdx = 0x140000000
1406b1517: movzx eax, byte ptr [rdx + rax + 0x6b338c]  ; byte compression table
1406b151f: mov  ecx, dword ptr [rdx + rax*4 + 0x6b3364] ; int32 offset table
1406b1526: add  rcx, rdx
1406b1529: jmp  rcx
```

RavelSignal (effectType `0x51`, index `0x50`) maps to the low-range dispatch
epilogue at `0x1406b321e` - the same default path as `SpellForceRemove`,
`Stealth`, and most other types in that range. Only effectTypes 4, 5, 6, 8,
and 27 have dedicated handlers in the low-range table; all others, including
RavelSignal, fall through to the shared epilogue. This means the client routes
RavelSignal entirely through the Ravel/Lua scripting runtime rather than a
dedicated C++ spell-effect handler. (The actual CRavel component offset is not
yet mapped - see **Remaining unknowns** below.)

**Server implementation** (verified with `dotnet build`; 200/200 tests pass):
- `IWorldEntityScript.OnSignal(uint signalId)` - new script callback (default
  no-op); implementing this in a C# entity script fires when any
  `RavelSignal` effect targets that entity.
- `IWorldEntity.SendSignal(uint signalId)` / `WorldEntity.SendSignal` - invokes
  `IWorldEntityScript.OnSignal` via the entity's script collection.
- `HandleEffectRavelSignalCore` now calls `target.SendSignal(ravelSignal.SignalId)`.
- `SpellEffectDiagnostics.TraceRavelSignal` - `skippedReason` parameter removed
  since dispatch is now live.

**Remaining unknowns:**
- `RavelSignalSemantics.Mode` (DataBits00): values 1-5 dominant in the 5,262
  game-table rows. Mode is structurally decoded but not semantically confirmed.
  The target entity is already resolved by the spell targeting system before the
  effect handler runs, so Mode may select a sub-receiver within the CRavel
  component (own instance vs. group vs. summoner) rather than changing the
  C++-level dispatch. Current implementation ignores Mode and always calls
  `OnSignal` on the resolved target; refine when mode semantics are confirmed.
- CRavel class layout: the actual entity offset for the CRavel component is
  **unknown** - `entity+0x7928` was previously misidentified as CRavel but is
  the network connection handle (see `entity+0x7928` row in the field offset
  table below). The CRavel component offset remains unrecovered.

---

### entity+0x118 = Faction2 Component (Faction2Id); entity+0x140 = Creature2Id

**Discovery method:** Cross-reference of `Spell4ValidTargets_GetCriteriaById`
(`140240b40`) data source against WildStar client table schema, then
correlation of TargetGroup `type`/`data0..data6` values against known game tables.

**`Spell4ValidTargets_GetCriteriaById` source confirmed:**
`PTR_u_TargetGroup_140a6d930` ? loads from `DB\TargetGroup.tbl`. The returned row
pointer is the `int*` criteria struct passed to `ValidTargetsCriteria_Evaluate`.
TargetGroup schema: `(ID, localizedTextIdDisplayString, type, data0..data6)`.
`type` = checkType (1-13); `data0..data6` = up to 7 value IDs to match against.

**entity+0x118 = Faction2 component ? Faction2Id:**
- `EntityCriteria_GetFaction2Id` (`1403b49c0`) reads `[entity+0x118]` as a
  component pointer ? calls `vtable[+0x18]()` ? returns Faction2Id (int32).
  Returns 0 if the component pointer is null.
- checkType 3 (must-match) and 4 (must-not-match) compare the entity's Faction2Id
  against `data0..data6` values from the TargetGroup row.
- **Evidence:** All TargetGroup type=3/4 data values fall within Faction2 table ID
  range (min ID = 164): 166, 167, 170, 171, 189, 190, 192, 193, 218, 219, 248,
  249, 274, 278, 284, 307, 312, 340, 390, 430, 463, 473, 478, 498, 510, 518, 521,
  523, 631, 651, 652, 682, 776, 1112 - all verified as valid Faction2 IDs.
- **Distinction from checkType 1/2:** checkType 1/2 use FactionGroupId (`entity+0x18`
  faction-state struct, first dword) - coarser Exile/Dominion/Neutral grouping.
  checkType 3/4 use Faction2Id - a finer-grained specific faction affiliation from
  the `Faction2` hierarchy table.
- **NexusForever mapping:** `IWorldEntity.Faction1` and `Faction2` are `Faction`
  enum values whose underlying uint integers ARE Faction2 IDs (Faction.Dominion=166,
  Faction.Exile=167). `FactionSet` spell effect already stores/restores Faction2 per
  entity via `DataBits00`. `Faction2Entry` exists in GameTableManager.

**entity+0x140 = Creature2Id:**
- `EntityCriteria_GetCreature2Id_ListMatch` (`1403b4960`, vtable[+0x18]) and
  `EntityCriteria_GetCreature2Id` (`1403b4a00`, vtable[+0x40]) both read
  `entity+0x140` as int32.
- checkType 9 performs a list-match: entity's Creature2Id vs. data0..data6 (up to
  7 Creature2 IDs from the TargetGroup row).
- **Evidence:** All TargetGroup type=9 data values verified as valid Creature2 IDs:
  2202, 2203, 2277-2282, 2352-2353, 2365, 2385, 2410-2411, 2441, 3086, 3830,
  3944-3945, 4298, 4859, 4860, 4861, 6292, 6879, 6880, 6881, 6882.
  Creature2 ID 2441 = `[PH] Moonfrenzy Tunnel - Dynamic Event - Object`.
- Players return 0 for this field (no template creature ID for player characters).
- **NexusForever mapping:** `IWorldEntity.CreatureId` (uint) is the Creature2
  template ID. `IWorldEntity.CreatureEntry` is the resolved `Creature2Entry`.
  For non-player entities, `CreatureId` matches what entity+0x140 holds.

**type=12 UnitRaceId cross-verification:**
- TargetGroup type=12 values (5, 226, 232, 233) verified against UnitRace table:
  IDs 226 and 232 confirmed present. Confirms vtable[+0x38] = UnitRaceId mapping.

**Updated entity field offset table (complete):**

| Offset | Type   | Meaning | Source |
|--------|--------|---------|--------|
| +0x18  | ptr    | Faction-state component (first int = FactionGroupId) | EntityCriteria_GetFactionGroupId |
| +0xd0  | ptr    | UnitRace component (first int = UnitRaceId) | EntityCriteria_GetUnitRaceId |
| +0xd8  | int32  | RaceId | EntityCriteria_GetRaceId |
| +0xdc  | int32  | ClassId | EntityCriteria_GetClassId |
| +0x118 | ptr    | Faction2 component; vtable[+0x18]() = Faction2Id | EntityCriteria_GetFaction2Id + TargetGroup xref |
| +0x140 | int32  | Creature2Id (template/prototype creature ID) | EntityCriteria_GetCreature2Id + TargetGroup xref |
| +0x6420| bool   | Not-connected flag: set to `(entity+0x7928 == 0)` | Network_SendOpcodePayloadOrPackedHelper |
| +0x7928| ptr    | Network connection handle (session object); +0x98 = channel/session ID | Network_SendOpcodePayloadOrPackedHelper (1403f4740), Network_SendOpcodePayloadHelper (1403f4900), Entity_ExecuteSpellEffectHighRange (1403ec6a0) |
| +0x7b4c| uint32 | Timestamp/tick written before high-range spell effect dispatch | Entity_ExecuteSpellEffectHighRange (1403ec6a0) |

**Server implication:** No server code changes needed. All 13 checkType semantics are
now fully decoded. The existing `TargetBitmask` simplified projection remains the
verified server path. `entity+0x118` maps to `IWorldEntity.Faction1`/`Faction2` and
`entity+0x140` maps to `IWorldEntity.CreatureId` - both already exist on server entities.

---

### Spell4StackGroup ClientDB Query Thunks (P18 background)

`DAT_140c642f0` = global pointer to the loaded `Spell4StackGroup` ClientDB reader.
Populated by `ClientDB_RegisterSpell4StackGroup` (`14023afa0`) on first access.
Released by `ClientDB_FlushAllTables` (`14024e5e0`) during shutdown/reload.

Three standard ClientDB query thunks share the same hot-swap override pattern:

| Function | Address | Vtable slot | Semantics |
|---|---|---|---|
| `Spell4StackGroup_GetAllRows` | `14023b1b0` | `*DB + 0x28` (vtable[5]) | Returns all rows / row count - no ID parameter |
| `Spell4StackGroup_GetById` | `14023b200` | `*DB + 0x18` (vtable[3]) | Looks up one row by uint32 ID |
| `Spell4StackGroup_GetByIndex` | `14023b260` | `*DB + 0x20` (vtable[4]) | Looks up one row by sequential index |

Hot-swap override globals (DAT_140c63838 / 140c63840 / 140c63848):
Each thunk checks an override function pointer first - if non-null, the override is
called instead of the DB vtable. This is the standard WildStar ClientDB mock/hot-patch
hook used in testing. Under normal gameplay the overrides are null.

**P18 implication:** `Spell4StackGroup` arbitration on the client is entirely through
these thunks. The server-side `GameTableManager.Instance.Spell4StackGroup` already
loads the same table data. Implementing stack-group arbitration requires mapping the
server spell-instance lifecycle to match the group rules (stack limit, exclusive flags)
in `Spell4StackGroupEntry`; no new decomp is needed for the DB query side.

---

## Spell Network Opcodes (Additional)

Decoded from `SpellCast_SendClientToggleCastOn`, `SpellCast_SendClientToggleCastOff`,
`SpellCast_SendClientSpellStopCast`, `SpellEffect_SendClientCancelEffect`.

| Opcode (hex) | Direction | Name | Payload |
|-------------|-----------|------|---------|
| `0x17e` | Client?Server | ToggleCast | `{1}` = on, `{0}` = off; preceded by validation |
| `0x801` | Client?Server | SpellStopCast | `{entityId, reasonCode, 0}`; `reasonCode < 0x14c` has string mapping |
| `0x802` | Client?Server | CancelEffect | `{effectSequenceId, ...}` sent when cancelling effect type 0xe or 0x24 |

### Toggle Cast State
- `entity + 0x6820` = current toggle cast state: 0 = off, 1 = on

### Active Effect Struct Fields (param_2 in SpellEffect_SendClientCancelEffect)

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x5c` | uint32 | `effectSequenceId` | Sent in CancelEffect opcode payload |
| `+0x138` (`[0x27]`) | pointer | `spellWrapper` | Spell wrapper pointer for this effect |
| `+0x158` (`[0x2b]`) | int32 | `casterEntityId` | Entity ID of the caster |

Cancellable effect types (via opcode 0x802): `0xe` (14) and `0x24` (36).
Targeting mode 10 requires the caster to match local player for cancel.

---

## QuestPrerequisite Faction Level Check

Decoded from `QuestPrerequisite_CheckFactionLevel @ ?`.

The Spell4Prerequisites row has 3 faction level check slots at row offsets:

| Row byte offset | Field | Notes |
|----------------|-------|-------|
| `+0xb0` | factionId[0] | 0 = skip this slot |
| `+0xbc` | factionId[1] | |
| `+0xc8` | factionId[2] | |
| `+0xb0+0x0c` | minLevel[0] | Required faction level |
| `+0xb0+0x18` | direction[0] | 0 = must be >= minLevel; non-zero = must be < minLevel |

Iterates prerequisite row offsets 0xb0..0xbc (3 slots x 4 bytes apart).
`(*factionObj + 0x28)(factionObj)` = vtable GetFactionLevel.


---

## SpellWrapper InnerData - Additional Fields

Decoded from `ActionSet_ValidateRequestedChanges`, `ServiceToken_HandleCastResult`.

Additional `inner` struct (at `*(wrapper + 0x70)`) fields:

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0xf8` | int32 | `primaryEffectType` | Primary spell effect type (e.g., 0xe, 0x24, 0x70) |
| `+0xfc` | int32 | `spellSlotType` | Type for ActionSet validation: 5 = ability (slots 0-7), non-5 = passive/AMP (slots 8-11) |
| `+0x108` | uint32 | `propertyFlags` | Full 32-bit flags: bit 0x02 = AoE ValidTargets bypass; bit 0x20000000 = ServiceToken required |

Note: `+0x108` was previously documented as a byte with only bit 0x02; it is a full uint32.

## Entity Struct - Additional Fields

From `ActionSet_SendPendingActionSetChanges`, `CSIAction_HandleCurrentTargetApproach`,
`TargetThreatList_RebuildAndDispatch`, `ActionSet_ValidateRequestedChanges`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x80` | int32 | `entityType` | 0x14 = NPC, 0x17 = untargetable entity type |
| `+0x250` | int32 | `movementBlockFlag1` | Non-zero = entity blocked/obstacle (can't approach) |
| `+0x254` | int32 | `movementBlockFlag2` | Non-zero = entity blocked/obstacle (can't approach) |
| `+0x2ac` | int32 | `inCombatFlag` | Non-zero = in combat (blocks ActionSet changes) |
| `+0xaa8` | BST root | `actionSlotBST` | BST of SpellId ? action slot data (same structure as +0x7d18) |
| `+0x1460` | linked list | `actionSetChangeQueue` | Pending action set change entries |
| `+0x1468` | int64 | `actionSetChangeCount` | Count of pending changes in queue |
| `+0x66e4` | uint32 | `spellCastTimestamp` | Timestamp written when cast begins; `SpellCast_CancelCurrentCast` guards against cancels within 3000 ms of this value |
| `+0x66e8` | pointer | `threatListEntries` | Dynamic array of {entityId:uint32, threatAmount:uint32} pairs |
| `+0x66f0` | int64 | `threatListCount` | Count of entries in threat list |
| `+0x6dec` byte | byte | `actionSetStateByte` | Action set state flag byte |
| `+0xd50` | int32 | `approachEnabledFlag` | Non-zero = deferred target approach enabled |

## DeferredActionQueue Struct (at entity+0x70b0)

From `DeferredActionQueue_ArmTargetApproach`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x04` | int32 | `approachType` | 0xffffffff = none, 1 = approach target |
| `+0x08` | pointer | `approachData` | Cleared on reset |
| `+0x0c` | int32 | `targetEntityId` | Entity ID to approach |
| `+0x10` | int32 | `flag1` | Cleared on reset |
| `+0x3c` | float | `approachRange` | Default 1.5 (0x3fc00000); minimum approach distance |
| `+0x40` | int32 | `approachState` | Cleared on reset |
| `+0x48` | int32 | `approachTimeout` | Default 300 ms |
| `+0x4c` | int64 | `approachSlot` | -1 = any slot |
| `+0x54` | int32 | `approachSpellId` | 0xffffffff = any spell |
| `+0x80` | int32 | `flag2` | Cleared on reset |
| `+0x90` | pointer | `resetHandlerObj` | Vtable at +0x98 = reset callback |

## ActionSet Validation Rules

From `ActionSet_ValidateRequestedChanges`:
- Valid action set index range: 1..22 (param_3 - 1 in range 0..21)
- Must NOT be in combat (entity+0x2ac == 0)
- Exact 12 spells per action set (0x30 bytes = 12 x int32)
- Slots 0..7: spell `inner+0xfc` MUST equal 5 (ability type)
- Slots 8..11: spell `inner+0xfc` MUST NOT equal 5 (passive/AMP type)


---

## SpellWrapper InnerData - Innate Cost / GCD / RequiredLevel Fields

From `Lua_GameSpell_GetCasterInnateCosts`, `Lua_GameSpell_GetGCDTime`, `Lua_GameSpell_GetRequiredLevel`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x28` | uint32 | `gcdGroupId` | GCD group table ID (looked up via Spell4GcdData); 0 = no GCD |
| `+0xa8` | int32 | `innateCostType1` | First innate resource type (0..30); 0xf = mana/ChargeAmt reset |
| `+0xac` | int32 | `innateCostType2` | Second innate resource type (0..30) |
| `+0xb0` | int32 | `innateCostAmount1` | Amount of type1 resource consumed on cast |
| `+0xb4` | int32 | `innateCostAmount2` | Amount of type2 resource consumed on cast |
| `+0x190` | uint64 | `innateActivationThreshold` | Minimum innate value required to activate via AbilityBook |

GCD time lookup chain: `inner+0x28 ? Spell4GcdData row ? base GCD ms ? FUN_14046a760(entity+0x78, gcdGroupId, basems) = effective GCD remaining`.

## SpellWrapper Top-Level - Additional Fields

From `Lua_GameSpell_GetCooldownTime`, `Lua_GameSpell_GetCasterInnateRequirements`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x38` | ptr | `cooldownDataPtr` | Points to cooldown data; `[0]` = base cooldown ms (uint32) |
| `+0x40` innate data | | `innateData` | When `*(wrapper+0x40)` non-null: `+0x10` = innateRequirementValue1, `+0x14` = innateRequirementValue2 (int32 each; > 0 = active requirement) |

## AoE Data Struct (at `*(wrapper + 0x40)`)

From `Lua_GameSpell_GetCasterInnateRequirements`:

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x10` | int32 | `innateReqValue1` | First innate requirement value (> 0 = active) |
| `+0x14` | int32 | `innateReqValue2` | Second innate requirement value (> 0 = active) |

## Entity Struct - Additional Fields

From `SpellCast_SendClient0x009dVariant`, `Lua_AbilityBook_ActivateSpell`, `SpellTarget_ValidateTargetRelationship`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x24` | byte | `entityFlags` | Bit 0 (0x01) = player-controlled; blocks some faction bypass paths |
| `+0x1480` | BST root | `spellSelectionBST` | BST for opcode 0x9d: node `+0x20`=SpellId, `+0x24`=slotType (1=quest, 2=other) |
| `+0x6d10` | float | `fallbackPosX` | Fallback cast position X when no entity target for opcode 0x9d |
| `+0x6d14` | float | `fallbackPosY` | Fallback cast position Y |
| `+0x6d18` | float | `fallbackPosZ` | Fallback cast position Z |

## Entity+0x78 Context Sub-Struct Fields

`entity+0x78` = entity active spell/ability state pointer (non-null = entity active in world).

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0xdc` | int32 | `classId` | Entity class ID (0..22 max); used for spell level requirement lookup |
| `+0xa04` | float | `cooldownReductionMult` | Cooldown reduction multiplier (e.g., 1.0 = no reduction) |
| `+0x1608` | linked list | `activeCooldownList` | Active cooldown tracking list; entries: `+0x04`=type, `+0x08`=spellId, `+0x0c`=groupId, `+0x20`=timerPtr, `+0x88`=next |

## SpellService (DAT_140c65b70) - Additional Fields

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x58` | array | `classSpellLevelTable` | Array of ClassEntry (0x10 bytes each), indexed by classId 0..22; `[0]`=pairArrayPtr, `[8]`=count |
| `+0x788` | BST | `spellChainBST` | SpellId ? chain/variant data; used when `castMethod == 7` (freeform/chain spells) |

## Network Opcodes - Additional

| Opcode | Direction | Name | Payload |
|--------|-----------|------|---------|
| `0x9d` | Client?Server | SpellCastVariant | `{spellId:uint32, targetValidation:uint32, entityId:uint32, posX:float, posY:float, posZ:float}` |
| `0x17a` | Client?Server | ActivateSpell | `{hasTarget:byte, spellBaseId:uint32}` |

## SpellCast Return Codes - Additional

| Return Code | Name | Condition |
|------------|------|-----------|
| `0x0d` (13) | SpellNotAvailable | `FUN_1403a1630(entity, baseSpellId, 1) == 0` - spell unlock check failed |
| `0x59` (89) | InvalidTargetRelationship | Faction relationship mismatch in SpellTarget_ValidateTargetRelationship |

## ServerSpellList Packet Format

From `ServerSpellList_ReadPayload`, `_ReadSpellEntry`, `_ReadTierEntry`, `_ReadVariantEntry`.

```
ServerSpellList {
    spellCount: uint32 [32 bits]
    spells: SpellEntry[spellCount]          // 0x40 bytes each
}

SpellEntry (0x40 bytes) {
    +0x00: uint32 [32 bits]                 // SpellId (probable)
    +0x04: uint32 [32 bits]                 // Spell4BaseId (probable)
    +0x08: uint32 [32 bits]                 // field3
    +0x0c: uint16 [18 bits]                 // field4
    +0x10: byte   [1 bit]                   // isNew flag
    +0x14: uint32 [32 bits]                 // tierCount
    +0x18: ptr    ? TierEntry[tierCount]    // 0x18 bytes each
    +0x20: byte   [8 bits]                  // variantGroupCount
    +0x28: ptr    ? unknown[count]          // 0x1c bytes each
    +0x30: byte   [8 bits]                  // cooldownGroupCount
    +0x38: ptr    ? CooldownGroup[count]    // 0x20 bytes each
}

TierEntry (0x18 bytes) {
    +0x00: uint32 [32 bits]                 // spell/tier ID
    +0x04: byte   [8 bits]                  // byte field1
    +0x05: byte   [8 bits]                  // byte field2
    +0x06: uint16 [16 bits]                 // field3
    +0x08: nibble [4 bits]                  // field4
    +0x0c: byte   [8 bits]                  // variantCount
    +0x10: ptr    ? VariantEntry[count]     // 0x50 bytes each
}

VariantEntry (0x50 bytes) {
    +0x00: uint   [19 bits]                 // spellId (up to 524287)
    +0x04: uint32 [32 bits]                 // field2
    +0x08: uint32 [32 bits]                 // field3
    +0x0c: uint32 [32 bits]                 // field4
    +0x10: enum   [2 bits]                  // type (0 or 1; selects union branch at +0x18)
    +0x18: varies                           // union data based on type
}
```


---

## SpellWrapper Top-Level - Effect Array and Additional Fields

From `Lua_GameSpell_GetProxyChannelData`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x28` | int32 | `effectCount` | Number of SpellEffect entries in the effect array |
| `+0x30` | ptr | `effectEntries` | Pointer to SpellEffect array; stride = 0xa8 (168) bytes per entry |

### SpellEffect Entry (0xa8 bytes, within effect array)

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x10` | int32 | `effectType` | Spell effect type enum; 0x1a = ProxyChannel |
| `+0x40` | uint32 | `proxySpellId` | Valid when effectType == 0x1a; target proxy spell |

## SpellWrapper Inner Field Rename

`inner + 0xf8` confirmed by `Lua_GameSpell_GetClass`: this field is the spell **activation class** (an internal category enum, not character class). Values 0xe and 0x24 are treated as cancellable by `SpellEffect_SendClientCancelEffect`. Renamed from `primaryEffectType` to `spellActivationClass`.

## Entity Struct - Additional Fields

From `ActivateUnit_SendClientActivateUnitCast`, `DashCast_SendClientDashCast`, `Interaction_AttemptTargetAction`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x18` | ptr | `worldInstancePtr` | Non-null = entity is in world; vtable slot used in many checks |
| `+0x34ec` | byte | `debugFlag` | Non-zero triggers debug print in activate unit path |
| `+0x6648` | uint32 | `queuedCastTarget` | Cleared on cast failure; set to 0 |
| `+0x664c` | uint32 | `queuedCastTargetId` | Written with cursor target entity ID on cast failure |
| `+0x6650` | uint32 | `queuedCastSpellId` | Written with spell ID on cast failure |
| `+0x70b8` | uint32 | `deferredQueueApproachData` | DeferredActionQueue+0x08 flag; non-zero = clear queue on success |
| `+0x75d8` | uint32 | `dashLimitFlag` | Non-zero = dash not allowed (some restriction) |
| `+0x78d8` | uint32 | `dashActiveFlag` | 0 = not dashing, 1 = dash cast active |
| `+0x78e0` | uint32 | `dashStartTimestamp` | Game tick when dash started |
| `+0x78e8` | int32 | `dashDirection` | Direction/type of dash |
| `+0x7188` | ptr | `vehicleMountObj` | Vehicle/mount sub-object; vtable+0x68=get mount state, vtable+0x70=get mount entity ID |
| `+0x7190` | uint32 | `lastActivateTimestamp` | Game tick of last unit activation; rate-limits to 1000ms |
| `+0x7198` | ptr | `someMovementObj` | Sub-object; vtable+0x50 = some movement lock flag |
| `+0x7330` | ptr | `specialMovementState` | Passed to FUN_14055a260 (special movement state check) |

## Entity+0x1908 - Interaction Script Object

| Sub-offset | Type | Name | Notes |
|------------|------|------|-------|
| `[0]` (`+0x00`) | int32 | `interactionType` | 0x65 = blocked, 0x4d = class-restricted, other = open |
| `+0x04` byte | byte | `enabledFlag` | Non-zero = interaction available |
| `+0x09` byte | byte | `canDeferApproach` | Non-zero = allows DeferredActionQueue approach arm |
| `+0x0d` byte | byte | `hasPostFeedback` | Non-zero = calls Interaction_HandlePostActionTargetFeedback |
| `[4]` (`+0x10`) | float | `interactionRange` | Default 5.0 if 0 |
| `[0x10]` (`+0x40`) | ptr | `activationHandlerFn` | Function ptr for activation callback |

## CastContext Sub-Struct - Additional Fields

From `DashCast_SendClientDashCast`, `ActionSet_CheckUpdateSpellInProgress`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x358` | ptr | `dashStatePtr1` | Must be non-null for dash to proceed |
| `+0x4c8` | ptr | `dashBlockerPtr` | Must be null for dash to proceed |
| `+0x1070` | uint32 | `castModeFlags` | Bit 4 (0x04): blocks DeferredActionQueue dispatch; bit 0x100: required for dash; bit 0x200: main cast mode |
| `+0x1088` | int32 | `dashBlockFlag` | Non-zero = block dash cast |
| `+0x15c0` | linked list | `spellStateList` | Update-in-progress state chain; each entry has vtable+0x08 = GetStateId, entry+0x10 = next |

## Network Opcodes - Additional

| Opcode | Direction | Name | Payload |
|--------|-----------|------|---------|
| `0x94f` | Client?Server | GuildBossTokenCast | `{entryData, localSpellId, ..., timestamp}` |
| `0x97` | Client?Server | ActivateUnitCast | `{casterEntityId, targetEntityId, ...}` |
| `0xb3` | Client?Server | SelectTargetEntity | `{entityId:uint32}` |
| `0x35b` | Client?Server | QuestAccept | `{paramData, questId}` |
| `0x365` | Client?Server | QuestComplete | `{questId:uint32}` |

## SpellService - Additional Globals

| Address | Name | Notes |
|---------|------|-------|
| `DAT_140c65b80` | QuestServiceGlobal | `*DAT_140c65b80` = QuestRuntime pointer |
| `DAT_140c659c0` | ActionBarSlotTable | Slot count at `+0x10`, slot prereq array at `+0x08` (int32 each); up to 24 entries |
| `DAT_140c65b98` | MatchingGameMapService | `+0x108` = inMatchingGame flag; `+0x114` = matchType (1=rated, 2=arena) |
| `DAT_140c658d8` | DebugLogService | Used for debug output |
| `DAT_140c65990` | ?? | Called FUN_14049aa10(DAT_140c65990) in interaction success path |
| `DAT_140c7de18` | GuildBossTokenEntryList | Ptr to list; `DAT_140c7de20` = count |
| `DAT_140c635f0` | GameTimeGlobal | `+0x1680` = current game tick or timestamp |

## Return Codes - Additional

| Code | Name | Function |
|------|------|----------|
| 0x1f (31) | InvalidEntity | entity+0x18 == 0 or entity resolve failed |
| 0x5b (91) | EntityNotInWorld | entity+0x18 == 0 or interaction object has no world instance |
| 0x64 (100) | ActivationNotAvailable | Interaction script disabled or no handler |
| 0x106 (262) | ActivationRateLimited | Within 1000ms of last activation (entity+0x7190) |
| 0x13d (317) | CastQueued | SpellCast deferred/queued (not failed) |

## SpellService Functions - Additional Label

| Address | Name | Signature |
|---------|------|-----------|
| `1403ad690` | `SpellService_IsInRange` | `(castContext, targetEntity, minRange, maxRange, 0) ? bool` |


---

## Entity Struct - Additional Fields (Batch 3)

From `SpellCast_ValidateAndDispatch`, `SpellCast_ResolveTargetsAndValidate`, `SpellCast_SendClientToggleCastOn/Off`, `AbilityBook_SendClientCommitAmpSpec`.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0xc0` | int32 | `primaryCursorTargetId` | Cursor target entity ID tracked on entity; compared to castContext+0x08 |
| `+0x6364` | uint32 | `pendingFallbackSpellId` | Cleared on cast abort; set to current spellId under certain conditions |
| `+0x6718` | ptr | `activeCastContext2` | Second cast context ptr; non-null triggers opcode 0x18f notification |
| `+0x6820` | uint32 | `toggleCastActiveFlag` | 1 = toggle cast ON, 0 = toggle cast OFF |
| `+0x67b0` | uint32 | `substituteMountSpellId` | Pending auto-attack or mount substitute spell ID |
| `+0x6ce8` | uint64 | `substituteCastWrapperPtr` | SpellWrapper ptr for substitute cast (targeting type 4) |
| `+0x6cf0` | uint64 | `substituteCastFlag` | Value 0x100000000 when substitute active |
| `+0x6cf8` | uint64 | `substituteCastPositionPtr` | Position data for substitute cast |
| `+0x6d00` | uint64 | `substituteCastSlot` | Slot/count; high byte = 9 |
| `+0x6e70` | ptr | `ampSpecDataPtr0` | AMP spec array (uint16 entries) for action set 0 |
| `+0x6e78` | uint64 | `ampSpecCount0` | Count of AMP entries for action set 0 |
| `+0x6e80..6ea8` | - | `ampSpec1..3` | Repeat pattern for action sets 1-3: `+0x6e80/0x6e88`, `+0x6e90/0x6e98`, `+0x6ea0/0x6ea8` |
| `+0x6eb0` | ptr | `stagedAmpSpecPtr` | Staged/pending AMP node entries ptr |
| `+0x6eb8` | uint64 | `stagedAmpSpecCount` | Number of staged AMP entries (if non-zero, pending commit) |
| `+0x7040` | ptr | `specialResolvePath` | Non-null triggers FUN_14057a2c0 in target resolve |
| `+0x7258` | ptr | `vehicleOrMountStateObj` | `+0x14` = state type (3 or 8 = locked for toggle cast) |
| `+0x7ba0` | uint32 | `castingOptionFlags` | Bit 0 = self-cast mode; bit 1 = "self-cast mode" toggle (from Options_SendClientOptionsCasting) |

## Entity+0x78 Active State Sub-Struct - Additional Fields

| Sub-offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0xc0` | uint32 | `activeChannelTargetEntityId` | Entity ID of current channel/active cast target; checked in SpellCast_CancelCurrentCast and SpellCast_IsToggleSpellActive |
| `+0x1600` | uint32 | `activeToggleSpellId` | ID of the currently active toggle spell (non-zero if toggled on) |
| `+0x2ac` | int32 | `castUpdateInProgress` | Non-zero = cast or action-set update is in progress; blocks further casts |

## CastContext - Additional Fields

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x08` | uint32 | `cursorTargetId` | Primary cursor/tab target entity ID in cast context |
| `+0x37a0` | int32 | `serviceTokenCastBlocker` | Non-zero = block service token cast |
| `+0x2ac` | int32 | `updateInProgressFlag` | Same semantic as entity+0x78+0x2ac; shared flag across sub-systems |

## Entity World Instance Sub-Struct (entity+0x18 dereferences)

| Sub-offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x6c` | int32 | `dungeonInstanceFlag` | Non-zero = dungeon/instanced zone |
| `+0x148` | int32 | `raidInstanceFlag` | Non-zero = raid instance |
| `+0xc0` | ptr | `factionRelTablePtr` | Ptr to array of up to 4 faction relationship entries (each 4 bytes = criteriaId) |

## Entity Position Fields

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x11e0` | float | `posX` | World X position |
| `+0x11e4` | float | `posY` | World Y position |
| `+0x11e8` | float | `posZ` | World Z position |
| `+0x16f0` | ptr | `boundingVolumeObj` | Sub-object; vtable+0x50(1) returns bounds; `+0x30` = sphere radius |

## Entity Innate Ability Struct (entity+0x6d90)

From `Lua_GameLib_GetClassInnateAbilitySpells`, `Lua_GameLib_GetCurrentClassInnateAbilitySpell`.

| Sub-offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x08` | uint32[n] | `innateSpellIds` | Array of innate spell IDs; each 4 bytes |
| `+0x30` | int32 | `selectedInnateIndex` | Current selected innate spell index (0-based) |

## SpellWrapper Inner - Field Rename

`inner + 0xf8` was `primaryEffectType`; corrected name: `spellActivationClass` (returned by `Lua_GameSpell_GetClass`; values 0xe and 0x24 treated as cancellable by `SpellEffect_SendClientCancelEffect`).

## SpellService - Additional Fields

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| `+0x540` | hash_map | `spellWrapperByIdMap` | Hash map of Spell4 id -> spell wrapper; used by `SpellService_LookupSpellWrapperById` |

## Network Opcodes - Additional (Batch 3)

| Opcode | Direction | Name | Payload |
|--------|-----------|------|---------|
| `0xc2` (194) | Client?Server | ServiceTokenSpellCast | `{spell4BaseId:uint32, serviceTokenId:uint32}` |
| `0x12b` (299) | Client?Server | CastingOptionsUpdate | `{reserved:uint32, optionFlags:uint32}` where bit 1 = selfcast |
| `0x17e` (382) | Client?Server | ToggleCast | `{enabled:byte}` - 1 = on, 0 = off |
| `0x18f` (399) | Server?Client | SpellContextNotify | `{0:byte}` - sent when cast context changes |
| `0x1a2` (418) | Client?Server | CommitAmpSpec | `{count:byte, ampNodeIds[count]:uint16}` |
| `0x801` (2049) | Client?Server | SpellStopCast | `{reason:uint32, reasonAux:uint32, 0:uint32}` |

## Return Codes - ActionSet Specific

| Code | Name | Context |
|------|------|---------|
| 4 | InvalidSetIndex | ActionSet index out of range 1-22 |
| 0x16 | InvalidSpellCount | ActionSet must have exactly 12 spells |
| 0x17 | NullEntity | Entity ptr is null |
| 0x18 | SlotTypeMismatch | Slot 0-7 must be type 5; slots 8-11 must not be |
| 0x1e | UpdateInProgress | castContext+0x2ac is non-zero (action set update locked) |
| 0x14b | ServiceTokenInvalid | Service token expired or invalid timestamp |
| 0x80004005 | FeatureDisabled | Deferred action feature flag is disabled |

## DeferredActionQueue - Confirmed Full Layout (entity+0x70b0)

| Sub-offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x00` | uint32 | `type_high` | Unused / header |
| `+0x04` | int32 | `actionType` | 1=approach, 2=interaction/activate, 3=castViaActionSlot, 4=castViaSlotIndex; -1=idle |
| `+0x08` | uint64 | `reserved/cleared` | Reset to 0 on arm |
| `+0x0c` | uint32 | `targetEntityId` | Target entity for approach/interaction |
| `+0x10` | uint32 | `cleared` | Reset to 0 |
| `+0x3c` | float | `rangeParam` | 1.5f for approach; interaction-specific range otherwise |
| `+0x40` | uint32 | `cleared2` | Reset to 0 |
| `+0x48` | uint32 | `timeoutMs` | Default 300ms |
| `+0x4c` | int64 | `timedOut` | -1 = unset (not timed out) |
| `+0x54` | int32 | `slotOrRange` | -1 = unset; 1 = standard; interaction slot index |
| `+0x80` | uint32 | `cleared3` | Reset to 0 |
| `+0x90` | ptr | `subStateObj` | Sub-object (vtable+0x98 = clear sub-state) |

## AMP Spec System

- `entity + 0x6e70 + setIndex * 0x10` = ptr to AMP node ID array (uint16 per entry) for action set `setIndex`
- `entity + 0x6e78 + setIndex * 0x10` = count of AMP entries for that set
- `entity + 0x6eb0` = staged/additional AMP node ptr (pending additions)
- `entity + 0x6eb8` = staged AMP count (non-zero = commit needed)
- AMP nodes are uint16 (2 bytes each); up to 255 total per commit (count field is byte)

## BST Structure (spell wrapper BST at entity+0x7d18, action slot BST at entity+0xaa8)

From `Entity_LookupSpellWrapperInBST`, `ActionSet_SendPendingActionSetChanges`.

| BST Node Offset | Type | Name | Notes |
|----------------|------|------|-------|
| `+0x08` | ptr | `root` (sentinel) | Non-null = tree has entries |
| `+0x10` | ptr | `leftChild` | Smaller keys |
| `+0x18` | ptr | `rightChild` | Larger keys |
| `+0x20` | uint32 | `key` | Key (SpellId or slotIndex) |
| `+0x28` | ptr/val | `value` | SpellWrapper ptr or slot data ptr |

For the action slot BST (`entity+0xaa8`):
- Slot data at `*(node+0x28)`: `[1]` = count of action set entries; `*(slot + setIndex*8)` = per-set action entry
- Action entry: `+0x04` = spellId, `+0x08` = enabled flag (non-zero = slot unlocked)

## Labeled Functions - Additional

| Address | Name |
|---------|------|
| `1407a0fd0` | `SpellService_LookupSpellWrapperById` |


---

## Entity Struct - Additional Fields (Batch 4)

From `Lua_GameLib_*` and `SpellCast_SendClientToggleCastOn`, `Lua_GameLib_TogglePvpFlags`, `Lua_GameLib_GetRestXp`, etc.

| Byte offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x1688` | int32 | `restXp` | Current rest XP pool |
| `+0x6424` | int32 | `worldDifficulty` | World difficulty enum |
| `+0x6428` | int32 | `worldPrimeLevel` | Current world prime level |
| `+0x642c` | int32 | `worldForcesLevelScaling` | Non-zero = zone forces level scaling |
| `+0x6820` | uint32 | `toggleCastActiveFlag` | 1 = toggle ability currently on |
| `+0x6ee4` | int32 | `duelStateFlag` | 1 = pending duel request (can accept/decline) |
| `+0x7258` | ptr | `worldContextObjPtr` | World/zone context sub-object; `+0x14` = contextType (3 or 8 = action locked); `+0x54` = heroismMenaceLevel |

## Entity+0x78 Active State Sub-Struct - Additional Fields (Batch 2)

From `Lua_GameLib_SpendAttributePoints`, `Lua_GameLib_TogglePvpFlags`.

| Sub-offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x250` | int32 | `attributePointSpendBlocker` | Non-zero = cannot spend attribute points |
| `+0x15a8` | uint32 | `characterStateFlags` | Bit 1 (0x2) = actions blocked (dead/incapacitated) |

## Network Opcodes - Additional (Batch 4)

| Opcode | Direction | Name | Payload |
|--------|-----------|------|---------|
| `0xe8` (232) | Client?Server | DuelAccept | `{0:byte}` |
| `0xe9` (233) | Client?Server | DuelDecline | `{0:byte}` |
| `0xec` (236) | Client?Server | DuelInitiate | `{0:byte}` |
| `0x173` (371) | Client?Server | TogglePvpFlags | `{enabled:uint32}` |
| `0x17c` (380) | Client?Server | SpendAttributePoints | `{attrDeltas[n]:uint32}` variadic |

## Innate Ability System

- `entity + 0x6d90` = ptr to InnateAbilityState struct
  - `+0x08` = uint32 array of innate spell IDs (0-based, `+0x08 + index * 4`)
  - `+0x30` = selected index (int32, 0-based)
- `FUN_1405e7490(innateState, index, 1)` = sets selected innate ability index (0-based)
- `FUN_1405e73e0()` = returns count of innate ability spells
- `FUN_140469920(entity+0x78, spellId)` = checks if given spell is currently active/toggled on entity

## Challenges System

- `DAT_140c65948` = ChallengesService ptr; `+0x30` = challenges BST root
- Challenge entry (at BST node+0x28): `+0x48` = cooldownActive (0 = cooling down); `+0x34` = cooldownTimer (non-zero = in cooldown)
- Challenge data: `data[0]` = challengeId, `data[3]` bit 4 (0x10) = has cooldown type

## DB Table Addresses (ClientDB)

| Global | Table Name | Notes |
|--------|-----------|-------|
| `DAT_140c651c8` | Spell4HitResults | `DB\Spell4HitResults.tbl` |
| `DAT_140c63908` | Spell4ValidTargets | vtable+0x18 = GetById |
| `DAT_140c642f0` | Spell4StackGroup | vtable+0x18 = GetById, vtable+0x28 = GetAllRows |
| `DAT_140c65c20` | RewardRotationService | Reward rotation global; non-zero = loaded |


---

## SpellWrapper InnerData Layout - Expanded (Batch 2)

From `Lua_GameSpell_*` function analysis.

All offsets relative to `inner = *(longlong *)(wrapper + 0x70)`.

| Inner offset | Type | Name | Notes |
|-------------|------|------|-------|
| `+0x00` | uint32 | `baseSpellId` | Primary spell key; used in all lookups |
| `+0x04` | uint32 | `tierOrVariantId` | Tier/variant id; used for charge lookup |
| `+0x18` | int32 | `castMethod` | Cast method enum: 3=no-target, 7=threshold-accumulate |
| `+0x28` | uint32 | `gcdGroupId` | GCD group ID; lookup with `FUN_14023dc80` |
| `+0x78` | float | `targetAngle` | AoE cone angle (radians) |
| `+0x7c` | uint32 | `targetType` | 0,2,7=self; 1,3,5,8=cursor; 4=no-target/item |
| `+0x9c` | int32 | `selfSpellRestrictionFlag` | 0=normal self-target allowed |
| `+0xa8` | int32 | `cost1Type` | Innate cost 1 type (1-30; 0xf=innate resource) |
| `+0xac` | int32 | `cost2Type` | Innate cost 2 type |
| `+0xb0` | int32 | `cost1Amount` | Innate cost 1 amount |
| `+0xb4` | int32 | `cost2Amount` | Innate cost 2 amount |
| `+0xd0` | int32 | `reagentCount` | Total reagent count |
| `+0xdc` | uint32 | `reagentId1` | Reagent item ID slot 1 |
| `+0xe0` | uint32 | `reagentId2` | Reagent item ID slot 2 |
| `+0xe4` | uint32 | `reagentId3` | Reagent item ID slot 3 |
| `+0xf4` | int32 | `spellSchool` | Spell school enum |
| `+0xf8` | int32 | `spellActivationClass` | Activation class (0xe/0x24=cancellable) |
| `+0x108` | uint32 | `flagBits2` | Bit 1=AoE bypass; Bit 6=movingInterrupted; Bit 29=hasServiceTokenCost |
| `+0x10c` | uint32 | `flagBits3` | Bit 26=IsBeneficial; Bit 28=IsGroundTargeted |
| `+0x154` | uint32 | `miscFlags` | Bit 9=substituteCheck2; Bit 10=substituteCheck1 |
| `+0x168` | ptr | `casterCriteriaPtr` | Caster fails ? return 0x97 |
| `+0x16c` | ptr | `secondaryCriteriaPtr` | Secondary fails ? return 0x119 |
| `+0x180` | uint32 | `substituteSpellRef` | Substitute spell lookup reference |
| `+0x184` | uint32 | `thresholdTime` | Threshold cast time (uint32 ms) |
| `+0x198` | uint32 | `prerequisiteId` | Prerequisite ID (0=none); fail ? return 0x11 |
| `+0x1b0` | ptr | `questInteractionPtr` | Non-null ? quest accept/retry flow |
| `+0x1d0` | ptr | `tradeskillVendorPtr` | Non-null ? tradeskill vendor flow |
| `+0x1d8` | ptr | `tradeskillVendorPtr2` | Second tradeskill vendor check |

## SpellWrapper Object Layout - Full (Consolidated)

| Wrapper offset | Type | Name | Notes |
|---------------|------|------|-------|
| `+0x08` | ptr | `secondInnerPtr` | `+0x48` = castTimeOverrideMs (uint32) |
| `+0x28` | int32 | `effectCount` | Number of spell effects |
| `+0x30` | ptr | `effectEntriesPtr` | Array of 0xa8-byte effect entries |
| `+0x38` | ptr | `cooldownDataPtr` | `[0]` = cooldownMs; used in cooldown checks |
| `+0x40` | ptr | `innateRequirementsDataPtr` | `+0x10/0x14`=casterReqs; `+0x20`=targetReq |
| `+0x50` | ptr | `channelDataPtr` | `+0x00`=initialDelayMs; `+0x04`=maxTimeMs |
| `+0x70` | ptr | `innerDataPtr` | Primary spell behavior data (full layout above) |

## SpellEffect Entry Layout (0xa8 bytes per entry)

| Effect offset | Type | Name | Notes |
|--------------|------|------|-------|
| `+0x10` | int32 | `effectType` | 0x1a=ProxyChannel, 0x26=Tradeskill |
| `+0x40` | uint32 | `effectParam1` | ProxyChannel: proxySpellId; Tradeskill: tradeskillId |

## SpellService Internals (DAT_140c65b70)

| SpellService offset | Type | Name |
|--------------------|------|------|
| `+0x540` | hashmap | `spellWrapperByIdMap` |
| `+0x788` | BST | `selfSpellDelegateBST` |

## Entity+0x78 Active State Fields - Cooldown Tracking

| Sub-offset | Type | Name | Notes |
|------------|------|------|-------|
| `+0x0dc` | int32 | `abilityTierIndex` | Index for spell pricing lookup (range 0-22) |
| `+0xa04` | float | `cooldownScaleFactor` | Applied when `SpellWrapper_HasPlayerAbilityClass(wrapper) != 0` |
| `+0x1608` | ptr | `cooldownListHead` | Linked list of active cooldowns |

## Cooldown List Node Layout

Each cooldown node (linked list via `node + 0x88`):

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| `+0x04` | int32 | `type` | 1 or 2 |
| `+0x08` | int32 | `spellId` | Spell ID this cooldown is for |
| `+0x0c` | int32 | `secondaryId` | Matched against cooldownData secondary field |
| `+0x10` | struct | `timerData` | `FUN_140195f70(node+0x10)` = remaining ms |
| `+0x20` | ptr | `timerPtr` | Must be non-null for active cooldown |
| `+0x88` | ptr | `nextNode` | Next cooldown in linked list |

## New Function Labels

| Address | Name |
|---------|------|
| `1403ad860` | `SpellService_GetMinimumRange` |
| `1403ad8f0` | `SpellService_GetMaximumRange` |
| `14054e340` | `SpellInner_ResolveCastTimeMs` |
| `14046a890` | `SpellService_GetEffectiveCooldownMs` |
| `140195f70` | `CooldownNode_GetRemainingMs` |
| `14023dc80` | `SpellService_GetSpellCooldownEntry` |
| `14046a760` | `SpellService_GetGCDMilliseconds` |
| `1407a16f0` | `ChargeService_GetChargesForSpell` |
| `1405a4d90` | `SpellService_GetTokenCostValue` |
| `1405e73e0` | `InnateAbility_GetCount` |
| `1405e9400` | `SpellWrapper_ResolveLiveInstance` |
| `140462a90` | `Entity_GetInnateResourceValue` |


---

## SpellWrapper InnerData - Additional Flag Bits

From `Lua_GameSpell_IsFreeformTarget`, `ShouldHideCooldownInTooltip`, `GetRequiredWorldZone`.

**inner + 0x108 (flagBits2) - Updated:**
| Bit | Mask | Name |
|-----|------|------|
| 1 | 0x000002 | AoE bypass |
| 6 | 0x000040 | isMovingInterrupted |
| 22 | 0x400000 | isFreeformTarget (ground-target / free-position) |
| 29 | 0x20000000 | hasServiceTokenCost |

**inner + 0x10c (flagBits3) - Updated:**
| Bit | Mask | Name |
|-----|------|------|
| 9 | 0x200 | hideCooldownInTooltip |
| 26 | 0x4000000 | IsBeneficial |
| 28 | 0x10000000 | IsGroundTargeted |

**inner + 0x164 (new field):** `requiredWorldZoneId` (int32; 0=none)

## SpellWrapper secondInnerPtr Layout (wrapper + 0x08)

`secondInner = *(longlong *)(wrapper + 0x08)`

| Inner offset | Type | Name | Notes |
|-------------|------|------|-------|
| `+0x34` | uint32 | `lasTierDescStringId` | LAS tier description string ID |
| `+0x38` | uint32 | `lasBonusTierDescStringId` | LAS bonus-per-tier description string ID |
| `+0x48` | uint32 | `castTimeOverrideMs` | Override cast time in milliseconds |

## Entity Tradeskill Array

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| `entity + 0x1698` | ptr | `tradeskillListPtr` | Array of pointers to tradeskill entries |
| `entity + 0x16a0` | uint64 | `tradeskillCount` | Count of tradeskills |

Each tradeskill entry ptr: `entry + 0x08` = tradeskillId (int32)


---

## AbilityBook - Spell Activation and Tier System

From `Lua_AbilityBook_ActivateSpell`, `Lua_AbilityBook_UpdateSpellTier`.

### Entity Fields (Ability/Action System)

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| `entity + 0x6dd8` | uint32 | `currentActionSetIndex` | Active action set index |
| `entity + 0x6ddc` | uint32 | `pendingActionSetIndex` | 0xFFFFFFFF = none pending |
| `entity + 0x6dec` | byte | `activeActionSetByte` | Action set index as byte (same value) |
| `entity + 0x15f8` | uint64 | `abilityActivationThreshold` | Entity level/point threshold for ability activation |

### SpellInnerData

- `inner + 0x190` (offset 400) = `abilityActivationCost` (uint32) - must be ? entity+0x15f8

### ActionSet Slot Data

- `FUN_1403c1ea0(entity, slotIndex, actionSetByte)` = `ActionSet_GetSlotData` - returns slot data ptr
- Slot data `+0x118` = `slotSpellId` (uint32)

### Key Function Labels (AbilityBook)

| Address | Name |
|---------|------|
| `1404823c0` | `SpellWrapper_HasPlayerAbilityClass` |
| `1403bb170` | `SpellBook_IsSpellLearned` |
| `1403bb040` | `SpellBook_IsSpellSlotCompatible` |
| `1403bacc0` | `SpellBook_GetSpellTierData` |
| `1407a1440` | `SpellWrapper_BuildForTier` |
| `1403bb340` | `SpellBook_SendTierUpdateRequest` |
| `1403c1ea0` | `ActionSet_GetSlotData` |

## ActionSetLib - Slot Unlock System

From `Lua_ActionSetLib_IsSlotUnlocked`.

- `DAT_140c659c0` = `ActionSetSlotUnlockService` - slot unlock criteria table
  - `+0x08` = ptr to array of criteriaIds (int32 per slot)
  - `+0x10` = count (max slot index in table)
  - CriteriaId == 0 ? slot always unlocked (return 1)
  - CriteriaId != 0 ? checked via criteria vtable at DAT_140c659a0

Return value semantics:
- 1 = unlocked (no criteria or criteria passed)
- 2 = locked (has criteria; criteria not met)
- 0x17 (23) = no active entity state
- 0x18 (24) = slot index out of criteria table range

## Network Opcodes - Additional (Batch 4 cont.)

| Opcode | Direction | Name | Payload |
|--------|-----------|------|---------|
| `0x17a` (378) | Client?Server | ActivateSpell | `{spellId:uint32, isShiftHeld:byte}` |


---

## Network Opcodes - Additional (Batch 5)

| Opcode | Direction | Name | Payload |
|--------|-----------|------|---------|
| `0xb5` (181) | Client?Server | AssignMasterLoot | `{lootId:uint32, rollId:uint32, targetEntityGuid:uint128}` |
| `0x14c` (332) | Client?Server | RepairAllItems | `{vendorEntityGuid:uint64, 0:uint64, 0:uint64}` |
| `0x150` (336) | Client?Server | ResetAttributePoints | `{0:byte}` |
| `0x153` (339) | Client?Server | ResetSingleInstance | `{instanceId:uint32}` |
| `0x15d` (349) | Client?Server | LootRoll | `{rollId:uint32, lootId:uint32, rollType:uint32}` (rollType: 0=greed,1=need,2=pass) |
| `0x163` (355) | Client?Server | SetInstanceSettings | `{instanceId:uint32, difficultyId:uint32, levelScaling:uint64}` |
| `0x167` (359) | Client?Server | IsRepairVendorQuery | `{0:byte}` |
| `0x170` (368) | Client?Server | SetCharacterFlags | `{flags:uint32}` |
| `0x173` (371) | Client?Server | TogglePvpFlags | `{enabled:uint32}` |
| `0xd2` (210) | Client?Server | ConfirmInstanceSettings | `{instanceId:uint32}` |
| `0x830` (2096) | Client?Server | ReportBug | `{bugTypeId:ushort, itemId:uint32, zoneId:uint32, descriptionPtr:string}` |

## Entity Fields - Instance and Loot System

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| `entity + 0x6328` | uint32 | `characterFlags` | Bit 3 (0x8) = ignoreDuelRequests |
| `entity + 0x6640` | uint32 | `interactTargetEntityId` | Currently-interacted-with entity ID |
| `entity + 0x6644` | int32 | `interactType` | 0x31 = vendor/repair NPC |
| `entity + 0x7d84` | uint32 | `activeInstanceId` | Current instance ID for settings operations |
| `entity + 0x7d90` | ptr | `lootServicePtr` | Loot management sub-object |
| `entity + 0x6ee4` | int32 | `duelStateFlag` | 1=pending, 3=in duel |

## Loot Service Sub-Object (entity + 0x7d90 ptr)

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| `+0x28` | BST | `masterLootBST` | BST keyed by lootId; node `+0x2c` = rollId; `+0x28` = 8-byte loot entry |
| `+0x48` | BST | `rollLootBST` | BST keyed by lootId; node `+0x2c` = rollId; `+0x28` = loot data |
| `+0x68` | BST | `pendingMasterLootBST` | Pending master loot entries |

## Innate Ability Array Layout (entity + 0x6d90)

Confirmed from `Lua_GameLib_GetClassInnateAbilitySpells` and `GetCurrentClassInnateAbilitySpell`.

- Base: `entity + 0x6d90` = ptr to InnateAbilityState struct
- `InnateState + 0x08 + index * 4` = spellId array (iterates over count from `InnateAbility_GetCount`)
- `InnateState + 0x30` = selected index (int64)
- Pattern: stride 4 bytes per innate spell entry starting at `InnateState + 0x14` (`+0x08 + first index offset`)

## Entity Vendor Interaction Check

```
entity.interactType (entity + 0x6644) == 0x31 ? vendor/repair NPC
FUN_1403d90d0(entity, entity.interactTargetEntityId) ? resolve interacted entity
interactedEntity + 0x36d8 ? is repair vendor flag (non-zero = repairs available)
entity + 0x15f8 ? current currency/resource available for repair
```

## Global Pointers - Additional

| Global | Name | Notes |
|--------|------|-------|
| `DAT_140c636a8` | `gameServerTimeBase` | Time origin for loot roll timer |
| `DAT_140c63650` | `LuaWindowRegistry` | `+0x2f8` = window ptr array; `+0x300` = window count |
| `DAT_140c65c20` | `RewardRotationService` | Non-null = reward rotation loaded |


---

## Challenges System (Lua_Challenges_* batch)

### Challenges Object Layout

All Challenges functions resolve via:
```
FUN_140056ab0(param_1, 1, "Game.Challenges") ? handle
*(longlong *)(handle + 8) + 8 ? challengeDataPtr
```

**Challenge static data (`challengeDataPtr`):**

| Offset | Type | Name | Source |
|--------|------|------|--------|
| `+0x00` | uint32 | `challengeId` | `GetId`: `*challengeDataPtr` |
| `+0x04` | uint32 | `challengeType` | `GetType` |
| `+0x0c` | uint32 | `challengeFlags` | `IsTimeTiered`: bit 3 = isTimeTiered; bit 4 (0x10) = hasBeenOnCooldown |
| `+0x14` | uint32 | `rewardTrackId` | `GetRewardTrack` |
| `+0x18` | int32 | `completionTotal` | `GetCompletionTotal` |
| `+0x24` | int32 | `startLocationRestrictionId` | `GetStartLocationRestrictionId` |
| `+0x2c` | uint32 | `tiersArrayPtr` | `GetAllTierCounts`: `challengeDataPtr + 0x2c` = tier array start (stride 4, 0xFFFFFFFF = end sentinel); `challengeDataPtr[3]` = flags, `challengeDataPtr + 0x0c` = isTimeTiered bit |
| `+0x38` | uint32 | `zoneId` | `GetZoneInfo` |
| `+0x44` | uint32 | `nameStringId` | `GetName` |
| `+0x48` | uint32 | `descriptionStringId` | `GetDescription` |

**Challenge runtime state** (via `DAT_140c65948 + 0x30` BST, keyed by challengeId):

| Node offset | Name | Notes |
|-------------|------|-------|
| `node + 0x20` | key = challengeId | BST node key |
| `node + 0x24` | displayTier (int32) | `GetDisplayTier` |
| `node + 0x28` | runtimeStatePtr | |

**runtimeChallengeState fields:**

| Offset | Type | Name | Source |
|--------|------|------|--------|
| `+0x14` | int32 | `currentCount` | `GetCurrentCount` |
| `+0x1c` | int32 | `completionCount` | `GetCompletionCount` |
| `+0x28` | int32 | `currentTier` | `GetCurrentTier` |
| `+0x30` | int32 | `isActivated` | `IsActivated`: non-zero = active |
| `+0x34` | int32 | `cooldownTimerActive` | `IsInCooldown` secondary check |
| `+0x40` | uint32 | `durationMs` | `GetDuration` |
| `+0x48` | int32 | `cooldownType` | `IsInCooldown`: 0 = cooldown fully active |

**Timer type** (`GetTimer` logic):
```
if (runtimeState + 0x30) != 0: timerMode = 2 (active count-up)
elif (runtimeState + 0x34) != 0: timerMode = 4 (cooldown count-down)
FUN_14048dd20(runtimeState, challengeId, timerMode) ? remaining float
```

**All-tier info** (`GetAllTierCounts`):
- `challengeDataPtr + 0x2c` = start of tier goal count array (uint32 each, 0xFFFFFFFF = end)
- If isTimeTiered: goalCount in ms divided by 1000 for display
- `FUN_14048f880(challengeId)` = checks if challenge is in "max score" mode (goals shown as 100)

### Global: ChallengesService

| Global | Offset | Name |
|--------|--------|------|
| `DAT_140c65948` | `+0x30` | activeChallengesStateBST |
| `DAT_140c65948` | `+0xd0` | challengeDisplayTierBST |

---

## PublicEvent / PublicEventObjective System

### Globals

| Global | Name | Notes |
|--------|------|-------|
| `DAT_140c65980` | `PublicEventService` | `+0x30` vtbl slot = `GetObjectiveById`; `+0x00` vtbl slot = base; used by `FUN_140498a40` as `GetEventById` |

### PublicEvent Object (vtable interface)

Resolution pattern:
```
FUN_140056ab0(param_1, 1, "Game.PublicEvent") ? handle
*(longlong **)(*(longlong *)(handle + 8) + 8) ? publicEventObjPtr (has vtable)
(*vtbl + 0x20)(obj) ? getEventId() ? uint32
FUN_140498a40(PublicEventService, eventId, 0) ? liveEventObj (has vtable)
```

**PublicEvent vtable slot map:**

| VTbl offset | Function | Return | Source |
|-------------|----------|--------|--------|
| `+0x20` | `GetId()` | uint32 | `GetId`, `IsActive`, `GetElapsedTime` |
| `+0x28` | `GetEventType()` | int32 | `GetEventType` |
| `+0x38` | `GetTotalTime()` | int32 ms | `GetTotalTime` (requires IsActive) |
| `+0x48` | `GetObjectivesIterator()` | iterator | `GetObjectives` |
| `+0x68` | `IsActive()` | bool | `IsActive` |
| `+0x78` | `GetElapsedTime()` | int32 ms | `GetElapsedTime` (requires IsActive) |
| `+0x90` | `GetObjectiveByIndex(idx)` | objPtr | `GetObjective` |
| `+0xa0` | `GetRewardThreshold(tier)` | int32 | `GetRewardThreshold` |
| `+0xa8` | `GetRewardType()` | int32 | `GetRewardType` (requires IsActive) |
| `+0x170` | `GetJoinedTeam()` | int32 | `GetJoinedTeam` |
| `+0x178` | `GetTeamCount()` | int32 | `GetTeamCount` (requires IsActive) |

### PublicEventObjective Object (vtable interface)

Resolution pattern:
```
FUN_140056ab0(param_1, 1, "Game.PublicEventObjective") ? handle
*(longlong *)(*(longlong *)(handle + 8) + 8) ? objHandleData
*(uint **)(objHandleData + 8) ? objectiveData ptr
  objectiveData[0] = objectiveId (uint32)
  objectiveData + 0x04 = eventId (uint32)
  objectiveData + 0x14 = homeTeamIndex (int32)
  objectiveData + 0x18 = descriptionStringId
  objectiveData + 0x1c = altDescriptionStringId
(*DAT_140c65980 + 0x30)(service, objectiveId, 0) ? publicEventObjectiveObj (vtable)
```

**PublicEventObjective vtable slot map:**

| VTbl offset | Function | Return | Source |
|-------------|----------|--------|--------|
| `+0x28` | `GetParentEvent()` | eventPtr | `GetStatus`, `IsBusy`, `GetElapsedTime` |
| `+0x30` | `IsValidForStatusCheck()` | bool | `GetObjectiveType` |
| `+0x38` | `GetStatus()` | int32 enum | `GetStatus` |
| `+0x68` | `IsActive()` | bool | multiple |
| `+0x88` | `GetProgressFloat()` | float | `GetCount` when type==0x17 (contested) |
| `+0xa0` | `IsBusy()` | bool | `IsBusy` |
| `+0xa8` | `GetElapsedTime()` | int32 ms | `GetElapsedTime` |
| `+0x140` | `GetDescriptionStringId()` | ? | `GetObjectiveType` intermediate |
| `+0x150` | `GetObjectiveType()` | int32 | `GetObjectiveType`, `GetCount`, `GetRequiredCount` |
| `+0x180` | `GetTotalTime()` | int32 ms | `GetTotalTime` |
| `+0x1d8` | `GetRequiredCount2()` | int32 | `GetRequiredCount` |
| `+0x1e0` | `ShowHealthBar()` | bool | `ShowHealthBar` |
| `+0x1e8` | `IsHidden()` | bool | `IsHidden` |

**Objective types (from GetCount branching):**
- `0x17` = contested area (count = progressFloat x 100)
- `0x18`, `0x1b`, `0x1e`, `0x19`, `0x20` = various count-type objectives


---

## PathMission System (Lua_PathMission_* batch)

### Resolution and Vtable

All PathMission functions use:
```
FUN_14067b760() ? PathMission_ResolveCurrent() ? pathMissionRuntimeObj (vtable)
```

**PathMission vtable slots:**

| VTbl offset | Function | Return | Source |
|-------------|----------|--------|--------|
| `+0x08` | `GetSpellForMission()` | spellId | `GetSpell` |
| `+0x28` | `IsVisibleOrStarted()` | bool | `GetSettlerMayorInfo` guard |
| `+0x38` | `IsComplete()` | bool | `IsComplete` |
| `+0xd0` | `GetMissionState()` | int32 | `GetMissionState`, `GetType`, `GetNumCompleted`, etc. |

**GetMissionState return values:**
- 1 = not started/unavailable (returns 0 to Lua)
- 2 = in-progress/available
- 3 = started/active (`IsStarted` checks `==3`)

**`plVar4[6]`** (= `*(pathMissionObj + 0x30)`) = `pathMissionDataPtr`:

| Offset | Type | Name | Source |
|--------|------|------|--------|
| `+0x00` | uint32 | `missionId` | `GetId`: `*(int *)*pathMissionDataPtr` |
| `+0x0c` | int32 | `missionType` | `GetType`, type dispatch |
| `+0x10` | int32 | `displayType` | `GetDisplayType`; default -1 (`0xbff...`) |
| `+0x14` | uint32 | `subDataId` | Type-specific table lookup id |
| `+0x18` | uint32 | `nameStringId` | `GetName` |
| `+0x20` | uint32 | `episodeId` | `GetEpisode` |

### PathMission Type Constants (missionType at +0x0c)

| Value | Path | Sub-type | Lookup function |
|-------|------|----------|-----------------|
| `0` | Soldier | Holdout | `FUN_140617410(dataPtr, subDataId)` |
| `2` | Scientist | General | `FUN_14021fc40(subDataId)` ? scientist table entry |
| `4-6` | Settler | Varies | Settler-specific lookups |
| `0xe` | Scientist | Datacube | `FUN_14021fc40(subDataId)` |
| `0x16` | Scientist | Experimentation | `Service_LookupEntityById(ScientistExperimentationService, subDataId)` |
| `0x19` | Settler | Mayor | `DAT_140c65970` (SettlerPathService), `FUN_140222b00(subDataId)` |
| `0x1b` | Explorer | Node | `FUN_140721ef0(type, subDataId)` ? explorerData `+0x18` = nodeCount |

### Named PathMission Functions

| Address | Name |
|---------|------|
| `1404067b760` | `PathMission_ResolveCurrent` |
| `140491bd0` | `PathEpisode_LookupById` |
| `1403ba420` | `SpellWrapper_GetActiveForMission` |
| `14048d310` | `Service_LookupEntityById` |
| `140617410` | `SoldierHoldout_LookupById` |
| `140222b00` | `SettlerMayor_LookupById` |
| `140721ef0` | `ExplorerNode_LookupById` |
| `14021fc40` | `ScientistMission_LookupById` |
| `140200220` | `GameTable_GetEntryById` |

### Related Globals

| Global | Name | Notes |
|--------|------|-------|
| `DAT_140c65950` | `ScientistExperimentationService` | Used in experimentation lookup |
| `DAT_140c65968` | `PathEpisodeService` | Used in `GetEpisode` |
| `DAT_140c65970` | `SettlerPathService` | Used in settler mayor; `+0x08` must == 1 (settler path active) |

### Reward XP

`GetRewardXp` uses game table id `0x17a` (PathRewardTable); `entry + 0x04` = xpRewardAmount.
Default fallback = 50 (`0x32`).

---

## Network Opcodes - Batch 6 (GroupLib / CREDDExchange / Housing / Friendship)

| Opcode | Name | Payload Layout |
|--------|------|----------------|
| `0x267` | `CancelCREDDOrder` | `{0:u64, 0:u64, orderId:u64}` |
| `0x269` | `RequestExchangeInfo` | empty |
| `0x39e` | `FriendshipIgnoreStrangersState` | `{state:u32}` |
| `0x411` | `GotoGroupInstance` | `{groupInstanceId:u64}` |
| `0x412` | `SetInstanceDifficulty` | `{groupInstanceId:u64, difficultyId:u32}` |
| `0x513` | `NeighborInviteResponse` | `{accepted:u32}` (1=accept, 0=decline) |
| `0x515` | `NeighborEvict` | `{neighborId_lo:u64, neighborId_hi:u64}` (from `lVar1+0xb8/+0xc0`) |
| `0x518` | `NeighborSetPermission` | `{neighborId_lo:u64, neighborId_hi:u64, permissionLevel:u32}` |

---

## GroupLib System

### Entity Offsets
- `entity + 0x6c50` = groupContextPtr (null if not in a group)
- `entity + 0x6c10` = groupContextObj

### GroupContext Fields
- `groupContext + 0x48` = groupInstanceId (uint64)

### Named Functions
- `FUN_140601fb0(entity+0x6c10)` = `IsGroupLeader` - returns non-zero if local player is leader

---

## CREDDExchangeLib System

### Globals
- `DAT_140c635f0 + 0x1708` = creddPendingOrderFlag (non-zero if pending CREDD order exists)

### AccountItemLib Layout
- `DAT_140c635f0 + 0x15d0` = accountItemList
  - `accountItemList + 0x70` = count
  - `accountItemList + 0x68` = arrayPtr (stride 0x40 per AccountItem entry)
- Named call: `AccountItem_SendClientAccountItemTake(luaState, itemEntry)` - takes item from account

---

## GameContract System

### Lua_GameContract_Complete Pattern
- Resolves handle via `FUN_140056ab0(_, 1, "Game.Contract")`
- `*(handle + 8)` = contractRuntimeData
- `contractRuntimeData + 0x08` = questObjectiveNodeId
- Calls `QuestRuntime_FindObjectiveNodeById(_, questObjectiveNodeId)` to get objective node
- Type check: `FUN_1405a4850(_, 0x55)` = test if objective type == 0x55

---

## GalacticArchive System

### Architecture Overview
- `DAT_140c65898 + 0x78` = globalEntityPtr guard (must be non-null)
- `DAT_140c65990` = GalacticArchiveService (used for all article/entry lookups)
- `FUN_14048d310(servicePtr, id)` = **generic BST/service lookup by ID** - called as:
  - `GalacticArchiveService_GetArticleById(DAT_140c65990, articleId)`
  - `ScientistExperimentation_LookupById(DAT_140c65950, subDataId)` (same function, different service)
  - This is a generic helper; prior label `ScientistExperimentation_LookupById` was incorrect

### GalacticArchiveEntry Handle Resolution
```
FUN_140056ab0(luaState, 1, "Game.GalacticArchiveEntry") ? handle
*(handle + 8) = handleInnerData
*(*(handle+8) + 8) = dataPtr
GalacticArchiveEntry_ResolveStateObject(dataPtr, *dataPtr) ? stateObj
```

### GalacticArchiveEntry handleInnerData Vtable
- `+0x18` = GetStaticData (returns staticDataPtr)
- `+0x28` = GetTitleStringId (returns int32 string ID)
- `+0x30` = GetArticleId (returns int32 article ID)

### GalacticArchiveEntry stateObj Vtable
- `+0x18` = GetArticle (returns articleObj; push via `FUN_140432f20`)
- `+0x58` = GetIntegerField(fieldId) - returns integer IDs for:
  - `headerStyle`, `bodyStyle`, `headerCreature`, `iconId` etc. (client-defined field enum)

### GalacticArchiveEntry Named Functions
- `GalacticArchiveEntry_ResolveStateObject(dataPtr, *dataPtr)` = resolve state object from data ptr
- `GalacticArchiveEntry_CalculateProgress(stateObj, entryId)` = returns float progress (0.0-1.0)

### GalacticArchiveArticle Handle Resolution
```
FUN_140056ab0(luaState, 1, "Game.GalacticArchiveArticle") ? handle
*(handle + 8) = handleInnerData
*(*(handle+8) + 8) = staticDataPtr
```

### GalacticArchiveArticle handleInnerData Vtable
- `+0x18` = GetArticleStaticData (inner static data, used in GetText)
- `+0x28` = GetTitleStringId
- `+0x30` = GetArticleId

### GalacticArchiveArticle articleObj Vtable
- `+0x18` = GetArticleText (iterates up to 6 text entries)
- `+0x20` = GetStaticData (returns articleStaticData)
- `+0x48` = IsActive (returns bool)

### GalacticArchiveArticle staticData Fields
- `+0x04` = creaturePortraitId (int32)
- `+0x08` = iconPathStringId (int32)
- `+0x80` = worldZoneId (int32)
- `+0x88` = linkNameId (int32)

### Named Functions (Article)
- `FUN_14048d310(DAT_140c65990, articleId)` = `GalacticArchiveService_GetArticleById` (generic BST lookup)
- `FUN_140432f20(luaState, articleObj)` = `Lua_PushGalacticArchiveArticleObject`

---

## PublicEventObjective System

### Handle Resolution Pattern
```
FUN_140056ab0(luaState, 1, "Game.PublicEventObjective") ? handle
*(handle + 8) = handleInnerData
*(*(handle+8) + 8) = objData
*(objData + 8) = objectiveId (uint32)
(*DAT_140c65980 + 0x30)(DAT_140c65980, objectiveId, 0) ? objLivePtr
```
- `DAT_140c65980` = PublicEventService (confirmed; `+0x30` in service vtable = GetObjectiveById)
- Note: `+0x30` in *service* vtable vs `+0x28` in *event* vtable (GetObjectiveByIndex)

### PublicEventObjective liveObj Vtable Map
- `+0x28` = GetEventLiveObj (returns the parent PublicEvent live object)
- `+0x30` = GetStaticData (returns staticDataWrapper; `*(staticDataWrapper+8)+0x58` = displayOrder int)
- `+0x68` = IsActive (returns non-zero if objective is active/live; guards most reads)
- `+0x140` = GetDisplayData (returns object; `*(result+8)+0x58` = displayOrder)
- `+0x148` = GetProgressFloat (used in ShowPercent when not IsPercent mode)
- `+0x150` = GetObjectiveType (int) - known types: `0x17`=contested, `0x18`=countA, `0x19`=countB, `0x1f`=showRequired
- `+0x170` = GetTeamIndex (no IsActive guard; valid even if not fully active)
- `+0x1d8` = IsPercentMode (bool - if true, show as %; otherwise show raw count progress)
- `+0x1e0` = ShouldShowHealthBar (bool)
- `+0x1e8` = IsHidden (bool)
- `+0x1f0` = GetCategory (int)

### PublicEventObjective ShouldShowRequiredCount Logic
- Types `0x18`, `0x19`, `0x1f` ? show required count (default true)
- Other types ? check staticData via `+0x30` for an additional flag

### PublicEventObjective GetEvent / GetParentObjective
Both functions:
1. Call `+0x28` to get event live obj
2. Check `+0x68` (IsActive)
3. If active: call `+0x28` again to get event or parent; call into event vtable for further resolution

---

## PublicEvent System - Complete (34 functions)

### Standard Resolution Chain
```
entity+0x78 (guard) ? FUN_140056ab0(_, 1, "Game.PublicEvent") ? handle
*(handle+8) = handleInnerData
(*handleInnerData_vtable+0x20)(handleInnerData) = GetId ? eventId
FUN_140498a40(DAT_140c65980, eventId, 0) ? liveObj
(*liveObj_vtable+0x68)(liveObj) = IsActive
```

### handleInnerData Vtable
- `+0x18` = GetStaticData
- `+0x20` = GetId (returns eventId uint32)

### liveObj Vtable - Complete Map
- `+0x18` = GetEventData ? eventDataWrapper; `*(eventDataWrapper+8)+0x28` = flagBits uint32
- `+0x20` = GetStaticData ? `+0x1c` = parentEventId; `+0x24` = liveEventId
- `+0x28` = GetEventType (int)
- `+0x38` = GetTotalTime (ms, int32)
- `+0x68` = IsActive (bool guard)
- `+0x78` = GetElapsedTime (ms, int32)
- `+0x90` = GetObjectiveByIndex(idx) ? objectiveLivePtr
- `+0x98` = HasLiveStats (bool)
- `+0xa0` = GetRewardThreshold(thresholdIdx) ? thresholdObj
- `+0xa8` = GetRewardType (int)
- `+0x170` = GetJoinedTeam
- `+0x178` = GetTeamCount
- `+0x188` = ShowHintArrow (no return)

### liveObj flagBits (at `*(eventDataWrapper+8)+0x28`)
- Bit 12 = showMedalsUI for Exile path
- Bit 13 = showMedalsUI for Dominion path
- Bit 14 = isPriorityDisplay
- Bit 15 = shouldUseCustomTracker

### liveObj Stats Layout
- `liveObj + 0xec` = statsArray (inline, max 0xce = 206 entries)
- `FUN_1405f8a80(liveObj+0xec, statId)` = `PublicEventStats_LookupByStatId` - returns stat value

### Parent/Child Event Relationships
- `liveObj->GetStaticData()+0x1c` = parentEventId (0 if none)
- `liveObj->GetStaticData()+0x24` = liveEventId (0 if none)
- `FUN_140688bf0(luaState, parentEventId)` = push parent PublicEvent as Lua userdata
- `FUN_14020fd40(liveEventId)` = `LiveEvent_LookupById`
- `FUN_1406b91f0(luaState, liveEventObj)` = push LiveEvent as Lua object
- `*(liveEvent+0x0c) & 8` = some LiveEvent active/valid flag

### PrepareInfractionReport
- `liveObj + 0x1c8` = objectiveCount (used for bounds check on report building)

### RequestScoreboard (Window Registry)
- `DAT_140c63650 + 0x2f8` = LuaWindowRegistry array (ptr array of window ptrs)
- `DAT_140c63650 + 0x300` = LuaWindowRegistry count
- Each entry: `*(windowPtr + 400)` = associated Lua state ptr

### ShouldShowMedalsUI
- `entity + 0x6424` = playerPathType (Soldier=0, Settler=1, Scientist=2, Explorer=3)
- Exile path: flagBit 12; Dominion path: flagBit 13

### GetRewardThreshold
- Takes 2nd Lua arg as threshold index
- Calls `liveObj_vtable+0xa0(liveObj, thresholdIndex)` ? thresholdObj

### Named Functions (PublicEvent)
- `FUN_140498a40(DAT_140c65980, eventId, 0)` = `PublicEventService_GetLiveEventById`
- `FUN_1405f8a80` = `PublicEventStats_LookupByStatId`
- `FUN_140688bf0` = `Lua_PushPublicEventObjectByParentId`
- `FUN_14020fd40` = `LiveEvent_LookupById`
- `FUN_1406b91f0` = `Lua_PushLiveEventObject`

---

## HousingLib System

### Housing Neighbor Opcodes (see Batch 6 above)

### Entity / Neighbor Data
- `FUN_1404b7220()` = `Housing_GetCurrentNeighborSelection` - returns null if no selection
- neighborObj: `+0xb8` = neighborId_lo (u64), `+0xc0` = neighborId_hi (u64)
- `entity + 0x6838` = friendship/housing subsystem ptr (auto-response messages, neighbor list)
- `FUN_1405df7c0(entity+0x6838)` = check if friendship service is connected/ready

### GameHousingPlot Functions
- `FUN_140056ab0(_, 1, "Game.HousingPlot")` ? plotHandle; `*(plotHandle+8)` = plotId (uint32)
- Named calls: `Housing_SendClientPlugRemove(luaState, plotId)`, `Housing_SendClientPlugRepair(luaState, plotId)`
- `SetPlugRotation` uses: `FUN_1400f26a0(windowCtx+0x180, 2)` = get float arg from Lua

### Friendship Ignore Strangers
- `entity + 0x6ae0` = current ignoreStrangers state (int); only sends opcode if changed
- `FUN_1405df7c0(entity+0x6838)` = `Friendship_IsConnected`
- `Friendship_SetAutoResponseMessagesAndSend(entity+0x6838, msg1, msg2)` = named call

---

## ActionSetLib System

### ActionSet Globals
- `DAT_140c659c0` = ActionSetSlotUnlockTable (`+0x08` = slotDataArrayPtr; `+0x10` = slotCount)
  - Each entry is uint32 (0=always unlocked; non-zero = item/tier requirement)
- `DAT_140c659a0` = ActionSetService (vtable `+0x18` = CheckSlotRequirement)

### IsSlotUnlocked Return Values
- `0x18` = slot out of range (error)
- `0` = slot not found
- `1` = always unlocked (requirement == 0)
- `2` = locked (requirement not met)
- `3` = unlocked (requirement met)

### entity + 0x6490 Guard
- `entity + 0x6490` = trading/combat system ptr (non-null if in trade/combat)
- `*(entity+0x6490 + 0x2ac)` = flag that blocks ability changes (e.g., in combat)

### entity + 0xa90 / 0xa98 (IsNew / NewSpells Tracking)
- `entity + 0xa90` = newSpells array ptr (ptr array, each `+0x40`=spellId, `+0x48`=isNewFlag)
- `entity + 0xa98` = newSpells count

### ClearCachedLASUpdates
- Clears `entity + 0x1458` (LAS pending update cache)
- Resets `entity + 0x6ddc` = 0xffffffff (dirty/invalid sentinel)

---

## GameSpell / SpellWrapper System

### Globals
- `DAT_140c65b70` = SpellService (the primary spell data/wrapper service)

### Handle Resolution
```
FUN_140056ab0(luaState, 1, "Game.Spell") ? handle
*(handle+8) = handleInnerData
*(*(handle+8)+8) = spellId (uint32) - first field of innerData
SpellService_ResolveSpellWrapper(DAT_140c65b70, spellId, entity) ? spellWrapper
```
- `SpellService_ResolveSpellWrapper` is already named in the codebase
- `FUN_1405e9400(luaState, 1)` = alternate resolver used by cooldown/GCD functions (live instance)

### SpellWrapper Layout (via `lVar4 = spellWrapper`)
- `lVar4 + 0x70` = ptr to Spell4 static data (spellDataPtr - points to full Spell4 struct)

### Spell4 Static Data Fields (via `*(longlong *)(spellWrapper + 0x70)`)
| Offset | Type | Field | Source function |
|--------|------|-------|----------------|
| `+0x00` | int32 | spellId | `GetId` |
| `+0x04` | int32 | baseSpellId | `GetBaseSpellId`, `IsNew` |
| `+0x08` | byte | tier | `GetTier` |
| `+0x18` | int32 | castMethod (value 7 = freeform) | `GetCastMethod`, `IsFreeformTarget` |
| `+0x78` | float | targetAngle | `GetTargetAngle` |
| `+0xf4` | int32 | school | `GetSchool` |
| `+0xf8` | int32 | classRequirement | `GetClass` |
| `+0x108` | uint32 | interruptAndCostFlags (bit 29 = hasServiceTokenCost) | `IsMovingInterrupted`, `GetSpellServiceTokenCost` |
| `+0x10c` | uint32 | flagBits (bit 26 = isBeneficial, bit 28 = hideCooldownInTooltip) | `IsBeneficial`, `ShouldHideCooldownInTooltip` |
| `+0x164` | int32 | requiredWorldZoneId (0 = none) | `GetRequiredWorldZone` |

### SpellWrapper Instance Fields (via `spellWrapper` directly, not `+0x70`)
- `spellWrapper + 0x50` = channelDataPtr (non-null if spell has channel/proxy channel data)

### SpellService Level Tier Table
- `DAT_140c65b70 + 0x58 + playerLevel * 0x10` = per-level data entry
- `entity+0x78 ? +0xdc` = playerLevel (int; 0-0x16 = levels 1-23 check in GetRequiredLevel/GetPrice)

### Additional Named Functions (GameSpell)
- `FUN_140564fb0(_, spellId)` = `SpellService_GetThresholdTimeEntry` - returns threshold data for a spell
- `FUN_14034bdd0(worldZoneId)` = `WorldZone_GetNameString` - returns zone name string
- `FUN_1405a4d90(_, spellId)` = `SpellService_GetTokenCostValue` - returns service token cost from `Spell4ServiceTokenCost`
- `FUN_140501210(luaState, tokenCostResult)` = `Lua_PushSpellTokenCostResult` - push token cost onto stack

### SpellService Range Functions
- `FUN_1403ad8f0(DAT_140c65b70, spellId, entity)` = `SpellService_GetMaximumRange` ? float
- `FUN_1403ad860(DAT_140c65b70, spellId, entity)` = `SpellService_GetMinimumRange` ? float

### Cooldown Layout (from live spell instance `lVar5 = FUN_1405e9400(...)`)
- `*(uint **)(lVar5 + 0x38)` = ptr to cooldown data; `*ptr` = base cooldown ms (uint32)
- `FUN_1404823c0(lVar5)` = `SpellWrapper_HasPlayerAbilityClass`; checks spellData+0x198 class/category values 1, 2, or 7 before player cooldown scaling
- `entity+0x78 ? +0xa04` = cooldown reduction float multiplier (applied if above check passes)
- `FUN_14046a890(entity, liveSpellInst, effectiveCooldownMs)` = compute final effective cooldown ms
- Result conversion: `(float)ms * 0.001` ? seconds

### Active Cooldowns (GetCooldownRemaining)
- `entity+0x78 ? +0x1608` = active cooldown linked list head
- Node fields: `+0x04` = cooldown type (type-1 < 2 to match); `+0x20` = cooldown data ptr; `+0x88` = next node ptr
- **CooldownNode inner timer** (pointed to by active list node `+0x20`):
  - `timer+0x04` = expiry tick (uint32); remaining ms = `timer[+0x04] - *(DAT_140c63728+0xe8)`
  - `DAT_140c63728+0xe8` = global game tick counter (uint32); incremented each frame
  - Access is Win32 mutex-protected inside `CooldownNode_GetRemainingMs`
- GCD time: `FUN_14023dc80(*(spellDataPtr+0x28))` = look up GCD group; `FUN_14046a760(...)` = get GCD ms

### GetIcon
- `FUN_1405645b0(spellWrapper)` = `SpellWrapper_GetIconPathString` ? returns string/object
- Then `FUN_14018f0e0(local_28, iconString)` ? char* at `*(result+8)`

---

## AbilityBook System

### Window Context Pattern (AbilityBook / ActionSet)
Many AbilityBook/ActionSet functions look up the window context:
```
(iterates DAT_140c63650+0x2f8 window array)
*(window + 0x180) = window-local Lua args/context object
FUN_1400f26a0(windowCtx + 0x180, argIndex) = get Lua arg from window context
```

### AbilityBook Globals
- `entity + 0x6490` = combat/trade system ptr (blocks spell activation when `+0x2ac != 0`)

### AbilityBook Named Function Patterns
- `GetAbilitiesList` / `GetAbilityInfo`: use window context + `FUN_1400f26a0` for args
- `ActivateSpell`: guards on `entity+0x6490 ? +0x2ac == 0`
- `UpdateSpellTier`: same guard + `ActionSet_CheckUpdateSpellInProgress()` additional check
- `ClearCachedLASUpdates`: clears `entity+0x1458`; resets `entity+0x6ddc = 0xffffffff`

---

## ICComm System

### Lua_ICComm Pattern
Both `SetReceivedMessageFunction` and `SetSendMessageResultFunction` follow identical logic:
1. `FUN_140056ab0(luaState, 1, "Game.ICComm")` - resolves ICComm channel handle (result unused beyond type check)
2. `FUN_140056bb0(luaState, 2)` - get 2nd arg (the callback function)
3. Check if 2nd arg is a function (`*(param_1+0x18)+0x28` type tag == 5)
4. If valid, copy function reference onto Lua stack and return it

### ICComm Note
These functions only validate and pass the callback - actual callback registration is internal.
The `Game.ICComm` handle type tracks per-channel state.

---

## Crafting System

### Lua_Crafting Functions (2 named)
- `GetSchematicInfo` and `AddCoordinateDiscoveryInfo` - internal crafting helpers
- Both use the standard window context pattern (`DAT_140c63650+0x2f8`)

---

## GameResidence / GameRecruitmentGuild / GameItemData

### Lua_GameResidence_PurchaseInteriorWallpaper / RemoveInteriorWallpaper
- `1406aca10` is the `Game.Residence.PurchaseInteriorWallpaper` binding. It
  reads six Lua wallpaper handles, validates each selected
  `HousingWallpaperInfo` id against the corresponding slot flag
  (`0x01/0x04/0x08/0x10/0x20/0x80`), and routes changed slots through the
  interior wallpaper sender for opcode `0x050D`.
- `1406acc30` is the `Game.Residence.RemoveInteriorWallpaper` binding. It
  accepts a slot index `1..6` and calls the one-slot sender at `1404b7bc0`,
  which emits `DecorType=3`, the selected hook index, and default
  `HousingWallpaperInfo` id `5` as `DecorInfoId`.

### Lua_GameRecruitmentGuild_GetDetailedGuildInfo
- Resolves handle via `FUN_140056ab0(_, 1, "Game.RecruitmentGuild")`
- Calls `RecruitmentGuild_SendClientGetDetailedGuildInfo()` - named
- Returns bool (non-zero = request sent successfully)

### Lua_GameItemData_GetVirtualItems
- Provides virtual item list; depends on game table service

---

## New Function Labels (Batch 6 - this session)

| Address | Proposed Label | Context |
|---------|---------------|---------|
| `1403ad860` | `SpellService_GetMinimumRange` | GetMinimumRange ? (service, spellId, entity) |
| `1403ad8f0` | `SpellService_GetMaximumRange` | GetMaximumRange ? (service, spellId, entity) |
| `1404823c0` | `SpellWrapper_HasPlayerAbilityClass` | GetCooldownTime / AbilityBook gates |
| `14046a760` | `SpellService_GetGCDMilliseconds` | GetGCDTime |
| `14046a890` | `SpellService_GetEffectiveCooldownMs` | GetCooldownTime |
| `1405645b0` | `SpellWrapper_GetIconPathString` | GetIcon |
| `1405e9400` | `SpellWrapper_ResolveLiveInstance` | GetCooldownTime/Remaining/GCD |
| `14023dc80` | `SpellService_GetSpellCooldownEntry` | GetGCDTime |
| `1404b7220` | `Housing_GetCurrentNeighborSelection` | NeighborEvict/SetPermission |
| `1405df7c0` | `Friendship_IsConnected` | SetPersonalIgnoreStrangersState |
| `140432f20` | `Lua_PushGalacticArchiveArticleObject` | GalacticArchiveEntry.GetArticle |
| `14020fd40` | `LiveEvent_LookupById` | PublicEvent.GetLiveEvent |
| `1406b91f0` | `Lua_PushLiveEventObject` | PublicEvent.GetLiveEvent |
| `140688bf0` | `Lua_PushPublicEventObjectByParentId` | PublicEvent.GetParentEvent |
| `1405f8a80` | `PublicEventStats_LookupByStatId` | PublicEvent.GetStat |
| `140498a40` | `PublicEventService_GetLiveEventById` | PublicEvent standard chain |
| `14077cce0` | `ExplorerNode_GetCurrentNode` | PathMission.GetMapIcon |
| `140720b10` | `ExplorerNode_GetDistanceAndDirection` | PathMission.GetDistance |
| `140721f50` | `ExplorerHunt_LookupById` | PathMission.GetScoutInfo |
| `1407209f0` | `ExplorerHunt_GetClueByIndex` | PathMission.GetClueInfo |
| `14021e2c0` | `ExplorerPowerMap_LookupById` | PathMission.PowerMap |
| `140220080` | `ScientistDatacubeDiscovery_LookupById` | PathMission.Scientist |
| `140220d40` | `ScientistFieldStudy_LookupById` | PathMission.Scientist |
| `140221180` | `ScientistSpecimenSurvey_LookupById` | PathMission.Scientist |
| `140222f40` | `SettlerSheriff_LookupById` | PathMission.Settler |
| `140223380` | `SettlerMission_LookupById` | PathMission.Settler types 4-6 |
| `1406195b0` | `ScientistExperimentation_Refresh` | PathMission.RefreshExperimentation |
| `14077d240` | `ExplorerClue_ShowHintArrow` | PathMission.ShowExplorerClueHintArrow |
| `14056b7b0` | `PowerMap_GetProgressFloat` | PathMission.PowerMap progress |
| `14056f370` | `PathMission_ShowHintArrow` | PathMission.ShowHintArrow |
| `140571400` | `PathMission_ShowChecklistHintArrow` | PathMission.ShowPathChecklistHintArrow |
| `1403d3470` | `Lua_PushSpellObject` | PathMission.GetSpell |
| `14056c2b0` | `PathMission_GetPositionHandle` | PathMission.GetDistance |
| `14024b980` | `PositionHandle_GetPosObject` | PathMission.GetDistance |

### Relabeling Completed
- Row `14048d310` was relabeled from the older
  `ScientistExperimentation_LookupById` hypothesis to `Service_LookupEntityById`.
  This is a generic BST/service lookup used across GalacticArchive,
  ScientistExperimentation, and others. It takes `(servicePtr, id)` and returns
  the matching object or null.

---

## Function Label Hygiene Pass

`Decomp/Analysis/function_labels.csv` was deduplicated after the spell, path,
public-event, and cooldown mapping batches left several repeated
`WildStar64.exe` address rows. The retained rows were rechecked against
`functions.csv` and `selected_decompiled.c`; a few names/comments were tightened
where the last exported name was too narrow:

- `14023dc80` -> `SpellService_GetSpellCooldownEntry`
- `1403ac780` -> `Entity_GetIndexedSlotEntry`
- `1403ad860` -> `SpellService_GetMinimumRange`
- `1403ad8f0` -> `SpellService_GetMaximumRange`
- `1403d90d0` -> `Entity_ResolveById`
- `14046a760` -> `SpellService_GetGCDMilliseconds`
- `14046a890` -> `SpellService_GetEffectiveCooldownMs`
- `1404823c0` -> `SpellWrapper_HasPlayerAbilityClass`
- `140498a40` -> `PublicEventService_GetLiveEventById`
- `14054e340` -> `SpellInner_ResolveCastTimeMs`
- `14055bdc0` -> `SpellTarget_ResolveTargetEntity`
- `1405a4d90` -> `SpellService_GetTokenCostValue`
- `1405e9400` -> `SpellWrapper_ResolveLiveInstance`
- `1407a0fd0` -> `SpellService_LookupSpellWrapperById`

The high-range spell-effect case targets at `1403ec9ff`, `1403eef04`,
`1403eeb67`, `1403f102f`, `1403f103d`, and the default target `1403f12d2`
were also removed from `function_labels.csv` because they are jump-table
targets inside `Entity_ExecuteSpellEffectHighRange`, not standalone function
entries. Their addresses remain documented in the RavelSignal dispatch
architecture section above.

`scripts/ApplyNexusForeverLabels.java` now preserves comma-containing comments
by treating everything after the name as the comment field, and it reports
duplicate rows for the current program/address during label application. No
runtime NexusForever behavior changed in this pass.

PublicEvent binding-helper follow-up mapped from this pass:

- Three remaining selected `Game.PublicEvent` helper functions now have durable
  labels. `Lua_RegisterPublicEventBindings` (`140687d40`) creates the
  `Game.PublicEvent` metatable, installs the method table rooted at
  `PTR_DAT_140c5ca00`, and registers `PublicEventParticipantRemoveReason_*`,
  `PublicEventRewardTier_*`, `PublicEventRewardType_*`,
  `PublicEventType_*`, and `PublicEventStatType` constants around
  `exports\WildStar64.exe\selected_decompiled.c:88865`.
- `Lua_PushPublicEventObject` (`140432c80`) wraps a live public-event pointer
  as a `Game.PublicEvent` Lua userdata and pushes nil when the supplied pointer
  is null. Existing callers include `Lua_PushPublicEventObjectByParentId`
  (`140688bf0`) and mapped PublicEventObjective parent/event paths.
- `PublicEventObjective_ResolveLiveObjectiveFromLua` (`14068d500`) resolves the
  first Lua argument as `Game.PublicEventObjective`, reads the objective id from
  the wrapper, and calls `PublicEventService.GetObjectiveById` through
  vtable slot `+0x30` with selector `0`.
- No NexusForever runtime change was added. These helpers strengthen the
  public-event Lua binding map, but they do not add new server-side mutation
  semantics for objective progress, visibility, scoreboard, rewards, or live
  event state.

PublicEvent userdata-helper follow-up mapped from this pass:

- Six adjacent selected `Game.PublicEvent`/`Game.PublicEventObjective` helpers
  now have durable labels. `Lua_PublicEvent_UnwrapLiveEventPointer`
  (`1404203c0`) and
  `Lua_PublicEventObjective_UnwrapLiveObjectivePointer` (`140420440`) resolve a
  typed Lua userdata argument and write the stored inner live-object pointer to
  native output storage.
- `Lua_PublicEvent_CheckedUnwrapLiveEventPointer` (`140527a10`) and
  `Lua_PublicEventObjective_CheckedUnwrapLiveObjectivePointer` (`140527b60`)
  validate a Lua value against the corresponding metatable before returning the
  stored live pointer. The two functions share the same stack/metatable
  comparison shape and differ only by `Game.PublicEvent` versus
  `Game.PublicEventObjective`.
- `Lua_PublicEvent_IsPublicEventUserdata` (`140687900`) and
  `Lua_PublicEventObjective_IsPublicEventObjectiveUserdata` (`140687b80`) push
  boolean results after comparing the first Lua value's metatable with the
  registered public-event metatable.
- No NexusForever runtime change was added. This pass only makes the Lua
  userdata conversion and type-checking bridge reproducible; it still does not
  prove server-side event or objective state mutation semantics.

WildStar PublicEventUnitPropertyModifier table-registration follow-up mapped
from this pass:

- `ClientDB_RegisterPublicEventUnitPropertyModifier` (`WildStar64.exe`
  `140229720`) now has a durable label. The function is selected by the
  `PublicEventUnitPropertyModifier` descriptor at `140a9c500` and the
  `DB\PublicEventUnitPropertyModifier.tbl` path at `140a9c5d0`, matching the
  already-mapped Houston64 table registration at `1400e8700`.
- NexusForever already models this client table as
  `Source/NexusForever.GameTable/Model/PublicEventUnitPropertyModifierEntry.cs`
  with `Id`, `PublicEventId`, `UnitProperty2Id`, and `Scalar` fields, so this
  pass records table ownership only.
- No NexusForever runtime change was added. This is mapped static table
  registration evidence, not verified public-event objective mutation or unit
  property application behavior.

Server opcode reader cluster follow-up mapped from this pass:

- `FUN_14006c290` registers the server-reader cluster around the spell/entity
  opcodes. The inspected registration block wires `0x0816` to `140095f30`,
  `0x0817` to `140095fb0`, `0x0818` to `140095e60`, `0x091B` to `140096120`,
  `0x0262` to `140096fa0`, and `0x093A` to `140097710`.
- The inspected bodies corroborate the existing NexusForever model names:
  `ServerSpellHierarchy_ReadPayload` reads three 18-bit Spell4 ids plus a
  32-bit casting id, `ServerSpellEventByte_ReadPayload` reads one 18-bit
  Spell4 id plus one byte, and `ServerSpellTargetInfo_ReadPayload` reads a
  casting id followed by the shared target-info payload helper.
- `ServerEntityCreate_ReadPayload` is the large `0x0262` reader with a
  0x120-byte registered object size. The inspected body reads an entity-type
  branch selector and nested arrays for cooldown/property/spell/movement-style
  payload sections, matching the guarded `ServerEntityCreate` model shape where
  spell initialisation data remains blocked unless mapped.
- `ServerCooldownList_ReadPayload` and
  `ServerEntityPropertiesUpdate_ReadPayload` are labelled from the same
  registration evidence plus caller traces over repeated cooldown/property
  records. No NexusForever runtime changes were added because these labels only
  make the native reader map reproducible.

Shared 32-bit-plus-flag server reader follow-up mapped from this pass:

- `ServerUInt32AndFlag_ReadPayload` (`14007c340`) now has a durable label. The
  inspected body null-checks its payload pointer, reads one 32-bit field at
  offset `+0`, then reads one trailing bit at offset `+4`.
- The `FUN_14006c290` registration block wires this same 8-byte reader to
  multiple server opcodes, including `0x0219` (`ServerCinematicPlayerControl`),
  `0x022B` (`ServerCinematic022B`), `0x0355` (`ServerEntityDestroy`),
  `0x0357` (`ServerDialogStart`), `0x06F5`
  (`ServerPublicEventBombDropped`), `0x0700`
  (`ServerPublicEventTriggerUiUpdates`), `0x0755` (`ServerUnitInUse`), and
  `0x089A` (`ServerUnitEnteredCombat`), plus several currently unnamed enum
  slots.
- No NexusForever runtime change was added. The shared wire shape is mapped,
  but the meaning of the trailing flag remains opcode-specific and should stay
  diagnostic until each consumer has direct context or sniff evidence.

Shared 18-bit server reader follow-up mapped from this pass:

- `ServerUInt18_ReadPayload` (`140080d30`) replaces the narrower
  `ServerOpcode0815_ReadSpell4Id` label. `InspectCodeAddress.java` shows this
  code stub null-checks the payload pointer, then jumps to the shared bit
  reader for exactly 18 bits with no trailing flag.
- The registration block wires the stub to more than `0x0815`: current
  witnesses include `0x0129` (`ServerUnlockMount`), `0x01AE`
  (`ServerUnlockVanityPet`), `0x0815`, and an unnamed `0x00B0` slot. This makes
  the field width mapped, but the field meaning remains opcode-specific.
- No NexusForever runtime change was added. The existing source name/comment for
  `0x0815` remains blocked by the broader spell-broadcast reconciliation notes
  until packet witnesses can prove the opcode semantics.

Shared two-uint server reader follow-up mapped from this pass:

- `ServerTwoUInt32_ReadPayload` (`14007a040`) replaces the narrower
  `ServerAccountItemCooldownSet_ReadPayload` label. The selected decompile
  shows a null check followed by two 32-bit reads at offsets `+0` and `+4`.
- The `FUN_14006c290` registration block reuses this 8-byte reader for many
  server opcodes, including `0x021A`, `0x0223` through `0x0225`, `0x0813`,
  `0x0819`, `0x08C7` (`ServerVehiclePassengerRemove`), `0x0970`, `0x0973`,
  and `0x0974`. The old account-item cooldown interpretation remains one
  opcode-specific consumer, not the generic helper name.
- No NexusForever runtime change was added. The field names remain
  opcode-specific; this pass only records the shared wire reader.

PublicEventsLib registration follow-up mapped from this pass:

- `Lua_RegisterPublicEventsLib` (`1407625f0`) now has a durable label. The
  selected decompile shows the function registering the `PublicEventsLib` Lua
  library through the shared Lua registration helper.
- A focused `DumpNearbyData.java` pass over `140b76100` shows the table starts
  with string pointer `140b4d988` (`GetActivePublicEventList`) followed by
  function pointer `140762360`, then a null terminator before the next
  contract-library callback table.
- The selected body for `Lua_PublicEventsLib_GetActivePublicEventList`
  (`140762360`) builds a Lua table by walking active public-event ids, resolving
  each matching live `PublicEvent` object, checking the corresponding
  `LiveEvent` table row, skipping rows with flag bit `0x08`, and pushing
  `Game.PublicEvent` userdata.
- No NexusForever runtime change was added. This maps the client Lua library
  surface only; active public-event state production remains governed by the
  existing public-event runtime and packet-model evidence.

Spell broadcast runtime listener follow-up mapped from this pass:

- `Entity_ExecuteSpellEffectHighRange` now gives the exact post-decode edges for
  the follow-up cluster: `0x0814 -> 1403ef384 -> 1403bee40`,
  `0x0815 -> 1403ef372 -> SpellThreshold_HandleClear`,
  `0x0816 -> 1403ef34e -> SpellThreshold_HandleStartWrapper`,
  `0x0817 -> 1403ef360 -> SpellThreshold_HandleUpdate`,
  `0x0818 -> 1403ee403`, and `0x0819 -> 1403ee3af`.
- `0x0818` now has a direct wrapper-runtime path. Case `1403ee403` resolves the
  leading `uint32` as a spell-wrapper id through
  `SpellService_LookupSpellWrapperByWrapperId`, then forwards the nested
  tier-entry payload into `SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast`.
  The nested tier-entry `dword @ +0x8` is the wrapper-local entity id used to
  create or update nodes under `spellWrapper + 0x48`, and the selector-specific
  variant tails feed `SpellWrapperNode_ApplyVariantEntry` instead of a generic
  spellbook or target-info consumer.
- `0x0819` is the paired wrapper-node cleanup path. Case `1403ee3af` resolves
  payload `[1]` as a spell-wrapper id, matches payload `[0]` against the node
  entity id under `spellWrapper + 0x48`, and then calls
  `SpellWrapperNode_PruneChildrenAndRefresh`.
- Runtime listener registration now maps to `DAT_140c65b70 + 0x650` with key
  `[type8Flag, routeKey, listenerCategory, listenerSubtype, slotOrBitIndex]`
  and route-descriptor payload pointer at node `+0x38`. The descriptor is
  consumed by `SpellRouteEvent_EmitOrQueue` (`140578460`), which either
  serializes a descriptor-kind-specific route event immediately or queues it on
  the entity when the entity is not ready. `SpellRouteEvent_DispatchByKind`
  (`140543630`) is now the safest label for the per-kind switch on
  `descriptor + 0x20`, while the shared initializers
  `SpellRouteEvent_InitCommonPayload` (`14054e9f0`),
  `SpellRouteEvent_InitEntityPayload` (`14054eb60`) and
  `SpellRouteEvent_InitExtendedVectorPayload` (`14054ee30`) confirm that the
  descriptor fields drive concrete outbound route-event payload shapes rather
  than acting as packet objects themselves. Full case coverage is now recovered
  for `0..0xc`: kinds `7`, `8`, `9`, and `0xc` all build payloads and then
  either attach `0x40`-byte deferred nodes to the target entity or hand them to
  the dedicated sink helpers at `140576f90`, `1405770f0`, `140577250`, and
  `140577510`, kind `0xa` is the direct `Spell4ClientMissile` path through
  `SpellService_LookupSpell4ClientMissileRecord` (`140237680`) and
  `SpellRouteEvent_HandleClientMissileRecord` (`1405458e0`), and case `0xb`
  is an explicit no-op in the current body.
- The wrapper route key at `spellWrapper + 0x348` is now sourced from the
  spell-service tuning tree at `DAT_140c65b70 + 0x598`, not from the listener
  registry. `SpellService_LookupSpellInnerTuningRecord` (`1407a1680`) matches a
  primary spell id plus one of four inner ids, and the same returned record also
  overrides minimum range, maximum range, and cast time. Wrapper setup
  `14053d1f0` copies record field `+0x28` into `spellWrapper + 0x348`, falling
  back to `innerData + 0x1c` when no override route key is present. This keeps
  the current business meaning of `+0x348` at the safer boundary: it is a
  per-spell route channel used to query runtime listeners, not a raw spell id.
- `spellWrapper + 0x358` is now reinforced as a wrapper-local expansion tree,
  not the global listener registry. The route-broadcast siblings use it only
  when descriptor flag `0x200` is set, at which point each local expansion node
  contributes an alternate route key from node `+0x24` plus a trailing selector
  from node `+0x3c` into `SpellRouteEvent_EmitOrQueue`.
- Descriptor field `+0x84` is now best described as the deferred-queue value
  for route listeners: non-zero entries are the subset materialized into the
  auxiliary queued tree at `DAT_140c65b70 + 0x658` rather than the immediate
  listener walk at `DAT_140c65b70 + 0x650`, and
  `SpellWrapper_BroadcastRouteListeners` (`140540430`) now shows that queued
  matches compute a wrapper progress scalar through
  `SpellWrapper_ComputeRouteProgressScalarByBitIndex` (`140540b30`) before
  storing the non-negative delta against `+0x84` into wrapper-local queued work
  records at `spellWrapper + 0x1b8/+0x1c0`.

Quest objective placeholder follow-up from source/runtime coverage:

- `QuestObjectiveType.Unknown10` has been renamed to
  `QuestObjectiveType.ScriptedTargetGroupChecklist`. The existing runtime
  already treated this objective type as both target-group backed and
  checklist-backed in `AssetManager` and `QuestObjective`, and the Northern
  Wilds `Q3487Shellshock` script updates it with the activated owner's
  `CreatureId` plus `QuestChecklistIdx`. The safer boundary is therefore a
  scripted target/checklist objective rather than a fully generic unknown
  quest type.
- No behavioral expansion was made in this pass; the rename documents behavior
  that was already implemented. Other `Unknown*`, `DataBits*`, `Client0x*`,
  and `Server0x*` placeholders remain blocked unless a future pass can tie
  their wire shapes to stable semantics. The movement/spline registration
  cluster currently provides payload layouts for several unresolved opcodes,
  but not enough evidence for safe semantic names.

Quest objective type crosswalk (client `QuestObjective.tbl`, build 16042,
verified 2026-05-23):

- Type `4` (`CollectItem`): `Data` matches `Item2Id` on 274/327 rows. Server
  now syncs progress from `Inventory.GetItemCount` on item create/stack gain and
  quest accept (`InventoryQuestObjectiveUpdater`).
- Type `25` (`CompleteEvent`) and type `31` (`Unknown31`): `Data` matches
  `PublicEventObjective.Id` on 111/142 and 206/257 rows respectively (not
  `PublicEvent.Id`). Server credits these when `PublicEventObjective` reaches
  `Succeeded` (`PublicEventQuestObjectiveUpdater`).
- Type `33` (`CraftSchematic`): `Data` matches `TradeskillSchematic2.Id` on
  recipe rows and overlaps `PublicEventObjective.Id` on event-craft rows.
  Server credits successful fixed-recipe crafts (`CraftingQuestObjectiveUpdater`)
  plus the public-event success path above.
- Type `23` (`ActivateTargetGroup`): already credited from spell activate
  effects; direct interaction now mirrors that path in
  `InteractionObjectiveUpdater`.
- Follow-up crosswalk (2026-05-23, second pass): types `20`, `28`, and `44` also use
  `PublicEventObjective.Id` as `Data` (not a separate combat-momentum table).
  Type `21` (`GatheResource`) uses `CreatureId` and is credited on kill/interaction.
  Type `42` (`EarnCurrency`) uses `CurrencyType` ids; type `40`
  (`ParticipateInGroupContent`) uses `MatchingGameType.Id`; type `46`
  (`KillCreature2`) uses `Creature2Difficulty.Id` (fix: do not key on `CreatureId`).
  Type `39` (`CompleteMaxLevelQuests`) is credited when a level-50 player completes
  a quest. Type `41` (`PvPKills`) is credited on player-versus-player kills.
  Type `48` (`BeginMatrix`) is credited from server-owned account primal-essence grants
  (Crimson/Cobalt/Viridian/Violet, ids 15-18) and quest-accept sync when essence is already
  held; client matrix UI open packets remain unmapped.
  Audit regeneration: `python Tools/WikiArchiveAudit/quest_implementation_audit.py`.

Evidence-backed restoration workboard follow-up:

- `plan-evidenceBackedRestorationEpic.prompt.md` now carries the active
  execution workboard for the restoration epic, including the placeholder
  baseline and the distinction between implemented, validation-pending, and
  blocked items.
- The Riders' Reef tutorial direction-table blocker is currently implemented in
  source: `QuestDirection` and `QuestDirectionEntry` are default `[GameData]`
  tables, and `Quest.SendObjectiveWorldLocationUpdates()` resolves direction
  world locations before indicator fallback. A focused regression test now
  guards the table-manager loading contract instead of widening tutorial
  runtime behavior. Verification: the focused
  `GameTableManagerGameDataContractTests` filter passed, then the full
  `NexusForever.Game.Tests` project passed with 316 tests, and
  `dotnet build Source\NexusForever.sln -v minimal --nologo` passed with 0
  warnings and 0 errors.
- Live smoke should now use the local client root `I:\WildStar`; that path has
  both `Client64\WildStar64.exe` and `Patch`. The NPE map asset
  `NewPlayerExperience.nfmap` is present and should be smoke-tested with
  `-ClientDirectory "I:\WildStar"`. No staged `*Ether*.nfmap` or
  `*Expedition*.nfmap` asset was found for the world `3404` expedition smoke,
  so that blocker is now narrowed to regenerating or locating the expedition map
  asset from `I:\WildStar\Patch` before running the live world `3404` flow.

Evidence-backed restoration runtime convergence follow-up:

- `MapGenerator` now uses the `SharpCompress` version supplied through
  `Nexus.Archive`, avoiding the local `LzmaStream` constructor mismatch during
  patch extraction. With that fix, world `3404` generation from
  `I:\WildStar\Patch` produced the staged map asset
  `.nexusforever-runtime\assets\map\ShiphandHungerFromtheVoid.nfmap`.
- Portable RabbitMQ startup now repairs reused volume ownership and the
  `.erlang.cookie` permissions before readiness checks. This unblocked the
  standalone local stack without requiring a manual Docker volume reset.
- Character EF migration metadata is complete for the three already-present
  2026 migrations: `ItemSoulbound`, `CharacterTradeskill`, and
  `CharacterSchematicArchive`. The local `nexus_forever_character` database now
  has `item.soulbound`, `character_tradeskill`, `character_schematic`, and
  `character_galactic_archive`. `dotnet ef migrations has-pending-model-changes`
  reports no Character or World model drift after the Character and World
  snapshot refreshes.
- The default runtime game-table contract now includes
  `ItemRandomStat`, `ItemRandomStatGroup`, `ArchiveArticle`, `ArchiveEntry`,
  and `ArchiveEntryUnlockRule`. This addresses the two observed WorldServer
  startup/enter-world crashes: null random-stat tables during item-info cache
  construction and null archive tables during galactic-archive initial packet
  generation. `ItemInfoPropertyBuilder` now also accumulates duplicate property
  budgets from standard and random-stat sources.
- The load-test login-world harness now writes GUID `Data2` and `Data3` as
  16-bit fields in `ClientHelloAuth` and reports `ServerAuthDenied` values
  explicitly. Before this fix Auth rejected the packet as `ErrorInvalidToken`;
  after the fix Auth returned a realm ticket and the harness completed through
  world entry.
- Local runtime verification used `I:\WildStar`,
  `.nexusforever-runtime\assets\tbl`, and `.nexusforever-runtime\assets\map`.
  The final run started Auth, STS, World, and RabbitMQ on ports `23115`, `6600`,
  `24000`, and `5672`, then
  `dotnet run --project Tools\NexusForever.LoadTest -- run login-world --user loadtest0001@example.local --password loadtest --samples 1 --warmup 0`
  passed with `1/1` success and artifact
  `artifacts\load-tests\login-world-20260520-181053.json`. A fresh post-run
  scan of NexusForever service logs found no `ERROR` or `FATAL` entries.
- World database verification recorded `Evil from the Ether.sql` in `version`,
  found `237` world `3404` `entity` rows, `12` world `3404` `entity_script`
  rows, and `431` world `3460` `entity` rows. Remaining world `3460` and world
  `3404` work is now manual client playthrough validation, not an automated
  backend startup blocker.

Rider's Reef hoverboard course follow-up:

- A live playthrough reached world location `51734` without teleporting, but
  character `19` remained on quest `10527` with objective `21323` still at `0`.
  The saved position was near the combat-projector landing spot while
  `WorldLocation2` `51734` has radius `1`, and the imported finish-line entity
  `73595` sits several meters away from the tiny world-location center. The
  runtime now applies a Rider's Reef-only recovery padding for `51734` in both
  immediate client movement updates and entered-world/relocate recovery, so the
  retail client landing spot advances the ride objective and exposes projector
  `73735`.
- The combat projector behavior remains mapped to the existing activation
  evidence: creature `73735` casts spell `87061`, queues the combat-projector
  cinematic, then transports Exile characters to `51739` and Dominion
  characters to `52898`. The projector is intentionally actionable only after
  the hoverboard projector objective and ride-finish objective are complete.
- Booster creatures `73461` were imported as simple entities and visible after
  the hoverboard course unlocked, but had no activate spell or script behavior.
  `Spell4` contains NPEU `Power Boost` spells for part 1 and hoverboard-related
  speed behavior, including mount-speed property changes. A narrow
  `TutorialHoverboardBoosterEntityScript` now attaches to creature `73461`,
  range-checks mounted tutorial players, and sends a forward velocity boost on
  the controlled hoverboard mover without changing global movement logic.

Rider's Reef post-hoverboard live-log follow-up:

- A fresh client pass with character `20` completed the shield/hoverboard
  projector path and reached the combat simulation, but the live
  `worldserver.log` showed three separate runtime faults. Booster contact raised
  `InvalidCastException` because `TutorialHoverboardBoosterEntityScript`
  resolved the player's `PlatformGuid` as `IUnitEntity`, while hoverboards are
  `MountEntity -> VehicleEntity -> WorldEntity`. The booster script now resolves
  and boosts an `IWorldEntity`, so the controlled mount receives the velocity
  command instead of throwing.
- The same pass force-disconnected during projector/objective handling with
  `InvalidOperationException: Collection was modified; enumeration operation
  may not execute` from `QuestManager.ObjectiveUpdate(...)`. Objective updates
  can complete a quest and move it between active/completed dictionaries while
  the manager is still iterating active quests. `QuestManager` now snapshots
  active quests for update, initial world-location packet emission, objective
  broadcasts, and `GetActiveQuests()` callers.
- A later disconnect path came from `ClientDashCast.Read(...)` reading a
  required 32-bit value from a short/empty body even though the handler only
  logs and ignores the packet. The model now treats a short body as the default
  state instead of throwing `EndOfStreamException`.
- Combat simulation clustering had two mapped contributors in the current data:
  the world import already contains the Exile first-wave battle beasts, mines,
  and turrets, while the map fallback was still adding extra Exile first-wave
  entities; and tutorial combat AI used the 50m leash as its initial aggro
  radius. The Exile fallback now only supplies the missing final wave
  (`73492`/`73567` near `51740`), while starter tutorial combat creatures keep
  the 50m leash but require a 14m initial aggro check unless they are directly
  damaged.
- Verification: `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo`,
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo`, and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  -v minimal --nologo` all passed. STS/Auth/World were restarted, ports
  `6600`, `23115`, `24000`, and `5000` are listening, and the fresh WorldServer
  startup section after `2026-05-20 20:28:29` has no `ERROR`, `Exception`, or
  fatal matches.

Rider's Reef heartbeat and diagnostics follow-up:

- A second live run still disconnected after the previous heartbeat workaround
  removal. The fresh `worldserver.log` showed character `20` force-logged out
  with `LogoutReason.AccountDisconnected` at `2026-05-20 20:48:56 UTC`, roughly
  300 seconds after world entry. The only notable inbound keepalive-like packet
  in that world session was unhandled `State(0x00000000)`; no
  `ClientPregameKeepAlive` packet appeared before the flatline. This maps the
  remaining disconnect to server-side liveness accounting, not to the earlier
  quest/projector exception path.
- Runtime heartbeat handling now treats any successful nonzero socket receive
  as liveness, logs explicit heartbeat flatlines with the 300s timeout, and
  downgrades expected socket-abort/read-reset disconnects to debug context
  instead of generic socket-read errors. `State` and `State2` are also registered
  as no-op heartbeat-refresh messages so the client packet that appeared in the
  log no longer lands in the unhandled-packet path.
- `WorldSession.OnDisconnect()` now logs account, character, player guid, world,
  zone, position, and remaining heartbeat before logout finalisation. If another
  disconnect happens, the log should now show whether it was a heartbeat
  flatline, client-side close/reset, or a gameplay exception.
- Starter tutorial `CombatAI` diagnostics were added for the known Rider's Reef
  combat creatures. The trace/debug path records load-time leash/aggro settings,
  accepted and rejected aggro decisions, target invalidation, chase decisions,
  and auto-attack cast/range skips, scoped to player-facing tutorial combat so
  global creature logs stay manageable.
- Loot tracing already had packet-shape diagnostics and client request-handler
  traces; this pass added the missing server-side decision trail. Creature and
  item loot generation now traces recipient selection, loot-table/direct-row
  rolls, rejected/deliverable items, generated loot instances, notify sends,
  collect/vacuum/roll/master-loot resolution, owner/range failures, loot-bag
  failure reasons, and per-item delivery/roll rejection details.
- Verification: `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo`,
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo`, and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed with
  `340/340` tests. The first parallel verification attempt hit transient C#
  compiler file locks only; the sequential rerun passed.

Rider's Reef loot and self-anchored combat validation follow-up:

- Fresh WorldServer log review after the loot diagnostics showed no `ERROR`,
  `FATAL`, or unhandled exception during the reported no-loot/combat window.
  Creature `73464` generated loot three times, but the server found no
  `entity_loot`/`loot_group` table rows and no imported `creature_loot` rows for
  that Creature2 id. A localhost `nexus_forever_world` query confirmed zero
  `creature_loot` and zero `entity_loot` rows for the known Rider's Reef combat
  creature ids checked (`73464`, `73465`, `73473`, `73492`, `73494`, `73566`,
  `73567`, `73735`, `74862`). Random OmniBit kill rewards still rolled and sent
  granted/explosion `ServerLootNotify` packets when the chance hit, so the
  static corpse-loot gap is data coverage, not a thrown runtime failure.
- The same log showed repeated player spell failures:
  `SpellDiagnostics primary-target-validation spell4Id=32893/32812 ... target=22
  result=TargetOrientation ... targetAngle=180 targetMechanic=2/16`, where
  caster `22` was also target `22`. Later successful casts from the same spells
  selected hostile targets through telegraph/effect targeting. This maps the
  player-facing "only attack when in front" symptom to primary-target angle
  validation being applied to the caster's own position for self-anchored
  telegraph spells. `Spell.CheckPrimaryTargetAngle` now skips angle checks when
  the resolved primary target is the caster; external primary targets still use
  the target-angle gate.
- Immediate granted loot now follows the already mapped loot-bag explosion path
  more closely: the delivered item updates inventory/currency first and then
  sends the granted/explosion `ServerLootNotify`, without also sending an
  individual `ServerLootGrant` for the same visual-shower item. This keeps random
  OmniBit kill notifications on the cleaner granted-notify path.
- Verification: `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed,
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed,
  and `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed
  with `345/345` tests. Earlier verification attempts were blocked only by the
  live WorldServer DLL lock and one self-inflicted parallel MSBuild `obj` lock.
  WorldServer was then relaunched as PID `31952`; ports `5000` and `24000` are
  listening, Auth/STs remain on `23115`/`6600`, and the fresh
  `NexusForever.WorldServer_20260520_31952.log` contains no log-level `ERROR`,
  `FATAL`, unhandled, or exception entries after startup.

Rider's Reef retail video starter-quest evidence follow-up:

- Retail YouTube evidence from `B1T4TRL9A_A` ("Navigating Nexus Quest
  Wildstar", uploaded `2016-06-08`) shows the hoverboard course unlocking after
  the hoverboard projector, then blue rings/arrows and gold booster pads carrying
  the player to the finish before combat-projector `Upload` transport.
  `o0ADNHDjDQ4` ("The Face of the Enemy Quest Wildstar", uploaded
  `2016-06-08`) is Dominion-side evidence: its first combat wave is Virtual
  Defense Daguns, while the Exile-side first wave remains the imported Dominion
  Battle Beast setup already present in world `3460`. The shared quest pacing is
  still first-wave kills, three mine detonations, two interrupt/destroy turret
  steps, then three elite kills; no corpse-loot interaction is visible in these
  clips.
- The latest local WorldServer log showed `TutorialHoverboardBoosterEntityScript`
  instances loading for creature `73461`, but no `booster applied` entries while
  the player reported visible boosters that did not accelerate. Hoverboards are
  controlled `MountEntity`/`VehicleEntity` movers, so the range event can arrive
  for the vehicle rather than the passenger player. The booster script now
  resolves either a direct player or the pilot of an `IVehicleEntity`, validates
  the same tutorial quest state, and applies the velocity boost to the actual
  mover. This keeps the faction-specific combat creatures unchanged while
  matching the retail booster-pad behavior seen in `B1T4TRL9A_A`.
- Verification: after stopping the old WorldServer PID `31952`, both
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  --no-restore -v minimal --nologo` and
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -v minimal --nologo` passed with `0` warnings and `0` errors.
  WorldServer was relaunched as PID `26228`; the fresh
  `NexusForever.WorldServer_20260520_26228.log` shows the rebuilt
  `TutorialHoverboardBoosterEntityScript` being discovered and contains no
  startup `ERROR`, `FATAL`, or exception matches in the checked window.

Rider's Reef mine and turret behavior follow-up:

- This pass did not add new native labels. The implementation is based on the
  retail video sequence in `o0ADNHDjDQ4` plus local table evidence for the
  starter mine spells: activation spell `85452` ("Dismantle Explosive Mine")
  emits RavelSignal and SetBusy effects, while the danger-zone spells are
  `85430` easy, `85629` medium, and `85630` hard with durations `4000`/`3000`/
  `2000` ms and radius telegraphs `1991`/`2440`/`2495`+`2496`.
- Runtime follow-up implemented from this pass:
  `TutorialCombatMineEntityScript` now attaches to combat mine creatures
  `73463`, `73667`, and `73668`. On a valid Rider's Reef combat-quest
  activation it marks the mine busy, broadcasts the mapped danger-zone spell
  start from the mine position with the corresponding telegraph data, then
  sends `ServerSpellGo`/`ServerSpellFinish` and clears the busy state after the
  table duration. The existing `InteractionObjectiveUpdater` mine-credit path
  remains responsible for quest progress, so the new script is visual/gameplay
  behavior rather than a second objective mutator.
- Turret follow-up implemented from this pass:
  retail evidence shows the turret step as a fixed interrupt/destroy station.
  `CombatAI` now treats Rider's Reef turret creatures `73494` and `74862` as
  stationary tutorial combat units: they still target and auto-attack when the
  player is in range, but they no longer run the generic chase loop that can
  make turret actors walk or clump with other mobs.
- Historical blocker resolved in the follow-up below:
  at this stage the mine script did not yet synthesize damage/combat-log payloads from the
  danger-zone `Damage` effects. The server can now show the mapped telegraph and
  explosion timing without guessing a non-unit mine caster or over-broadening
  spell runtime. The follow-up below closes this with mapped table values and
  decoded damage-payload evidence.
- Verification:
  isolated script build
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  --no-restore -p:OutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build-riders-reef-mines\script-main\
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed with `0`
  warnings and `0` errors. After stopping the old WorldServer PID `26228`,
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo`,
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo`, and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed
  with `345/345` tests. WorldServer was relaunched as PID `24760`; ports
  `5000` and `24000` are listening, the fresh
  `NexusForever.WorldServer_20260520_24760.log` shows
  `TutorialCombatMineEntityScript`, `TutorialCombatProjectorEntityScript`, and
  `CombatAI` discovered, and no startup `ERROR`, `FATAL`, unhandled, or
  exception matches were found.

Rider's Reef mine damage evidence and implementation follow-up:

- Native reader mapping now has durable labels for the damage payload path:
  `ServerSpellGo_ReadPayload` at `WildStar64.exe:1400953f0`,
  `ServerSpellEffectDamage_ReadPayload` at `WildStar64.exe:140095910`, and
  `SpellDamageDescription_ReadPayload` at `WildStar64.exe:1400946c0`.
  The decoded `ServerSpellGo` reader consumes server unique id,
  ignore-cooldown bit, primary destination, target-info list, initial-position
  list, telegraph-position list, missile list, and phase. The damage
  description reader consumes seven 32-bit damage values, a killed bit, 4-bit
  combat result, 3-bit damage type, and a trailing structure list. This matches
  the existing NexusForever `ServerSpellGo`/`TargetInfo.EffectInfo` model.
- Table evidence for the starter mine danger-zone spells is now sufficient to
  implement damage without inventing a fake unit caster. `Spell4Effects.tbl`
  maps `85430` easy to damage effect `225138`, physical damage `600`,
  `85629` medium to damage effect `225151`, physical damage `800`, and
  `85630` hard to damage effect `225155`, physical damage `1000`. Their
  telegraphs map through `Spell4Telegraph.tbl`/`TelegraphDamage.tbl` to circle
  radii `5`, `7`, and `9` respectively.
- `TutorialCombatMineEntityScript` now applies those mapped values on
  detonation to valid Rider's Reef combat-quest players inside the circular
  telegraph, consumes shields before health, and includes the mapped damage
  effect info in the emitted `ServerSpellGo` target-info payload. Damage remains
  scoped to the tutorial mine script rather than broadening generic spell
  runtime for non-unit simple-collidable casters.
- Verification: `run_ghidra_analysis.ps1 -Targets WildStar64.exe
  -MaxDecompiledFunctions 220 -ExportOnly -SkipCoverage` succeeded and applied
  labels; `functions.csv` and `selected_decompiled.c` contain the three new
  reader labels. Isolated Script.Main build, real Script.Main build, real
  WorldServer build, and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed
  with `345/345` tests. WorldServer was relaunched as PID `26696`; ports
  `5000` and `24000` are listening, the fresh
  `NexusForever.WorldServer_20260521_26696.log` shows
  `TutorialCombatMineEntityScript`, `TutorialCombatProjectorEntityScript`,
  `TutorialHoverboardBoosterEntityScript`, and `CombatAI` discovered, and no
  `ERROR`, `FATAL`, `Exception`, or `Unhandled` matches were found in the
  checked startup window.

Rider's Reef loot and mine activation log follow-up:

- The latest WorldServer log captured the uncollectable yellow "Unknown" drops
  around the starter mobs. The server granted account-currency omnibits through
  immediate loot, then sent a granted explosion `ServerLootNotify` for the same
  owner unit. The client rendered those account-currency entries as nearby loot
  objects with no Item2 name, then later sent `ClientLootItem` for client-side
  ids that the server had never registered as tracked loot. The server correctly
  logged those as unknown loot item requests, so the visible drops were a packet
  presentation bug rather than missing starter creature loot.
- `GlobalLootManager.GiveImmediateLoot` now suppresses granted explosion
  notifies for `LootItemType.AccountCurrency`. Omnibit rewards still use the
  direct `ServerLootGrant` delivery path, but no longer advertise fake
  collectable ground loot that cannot be looted by `V`, `F`, or click.
- The same log showed starter mine scripts loading and mine creatures becoming
  visible, but no `ClientActivateUnit`/interaction packets and no mine arm or
  detonation entries while the player tested them. The fallback simple-collidable
  mines now set `EntityCreateFlag.HasInteractionPrereq` on load so the create
  packet advertises them as client-interactable, matching the tutorial cinematic
  mine actors that already carried this flag.
- Verification: after stopping WorldServer PID `26696`, sequential
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo`,
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo`,
  focused loot tests for `GlobalLootManagerTests`, `LootInstanceDeliveryTests`,
  and `LootInstanceResolutionTests`, and the full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` all
  passed. WorldServer was relaunched as PID `33456`; ports `5000` and `24000`
  are listening, the fresh `NexusForever.WorldServer_20260521_33456.log` shows
  `TutorialCombatMineEntityScript`, `TutorialCombatProjectorEntityScript`,
  `TutorialHoverboardBoosterEntityScript`, and `CombatAI` discovered, and no
  `ERROR`, `FATAL`, `Exception`, or `Unhandled` matches were found in the
  checked startup window.
- A live follow-up on the relaunched process showed the loot fix in effect: the
  next starter kill logged direct account-currency delivery and `ServerLootGrant`
  only, with no granted `ServerLootNotify` ground-drop packet. The same window
  still showed repeated map tick stalls while trace logging was enabled, with
  bursts of entity/packet logging around player add and movement updates. The
  WorldServer NLog file and console targets now use async wrappers, the file
  target keeps the log file open, and concurrent file writes are disabled for
  the single-process target. This keeps trace diagnostics available while
  removing synchronous disk/console writes from the hot world tick path.
- Verification after the logging change: `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed with `0`
  warnings and `0` errors. WorldServer was relaunched as PID `30164`; ports
  `5000` and `24000` are listening, the fresh
  `NexusForever.WorldServer_20260521_30164.log` shows startup complete and the
  tutorial scripts discovered, and no startup `ERROR`, `FATAL`, `Exception`,
  `Unhandled`, or map-update warning matches were found in the checked startup
  window.

Rider's Reef Dominion departure table-evidence follow-up:

- Verification rejected the Exile-terminal fallback for Dominion Q10530. The
  runtime binary tables under `.nexusforever-runtime\assets\tbl` contain the
  exact Dominion departure rows: `Creature2` `74778` is Chancellor Juro
  Takigurian with display group `36035` and faction `170`, `74772` and `74773`
  are the Crimson Isle and Levian Bay departures terminals with display group
  `28562`, faction `219`, and activate spells `37270`/`50279`, and
  `74759..74766` are the Q10530 Dominion holotable checklist rows.
- The Q10530 chain and target groups also point at those Dominion rows:
  `Quest2` `10530` has prerequisite `10523` and objectives
  `21347..21351`; `TargetGroup` `14387` contains `74772,74773`; and
  `TargetGroup` `14386` expands through `14384`/`14385` to `74759..74766`.
  `WorldLocation2` places the terminals/checklist in Dominion departure zone
  `5999` at `52747`, `52748`, and `52749`, with Juro at `52750`.
- Older Dominion Arkship rows exist for Q7057 (`62234`, `33803`, `51014`), but
  those are less exact than the Q10530 NPEU part 5 rows. Runtime should keep the
  native Dominion IDs and should not auto-grant Q10530 simply on zone entry;
  the retail-aligned chain is still Q10523 completed -> Q10530 granted.
- `TutorialMapScript` now keeps the native Exile and Dominion final-departure
  IDs, spawns only the two target-group terminals for each faction, and places
  dynamic checklist entities around the target-group center world locations
  `51697`/`52749` instead of around the quest NPCs. The Q10528/Q10530 script
  comments were corrected to describe two terminals.
- Verification: a scratch `GameTable<T>` loader check against the binary `.tbl`
  files confirmed the rows above, and
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed
  with `0` warnings and `0` errors.

Rider's Reef final-departure checklist runtime follow-up:

- No new native labels were needed. This pass follows the table evidence above:
  Q10528/Q10530 final departure uses one target-group terminal objective plus an
  eight-entry `ActivateTargetGroupChecklist` objective (`14380` Exile,
  `14386` Dominion). NexusForever checklist progress is stored as bits from the
  activated entity's `QuestChecklistIdx`, matching the existing
  `QuestObjective.ActivateTargetGroupChecklist` implementation.
- Dynamic Rider's Reef fallback spawns now preserve the per-actor checklist bit
  index for the Exile checklist creatures `73677..73684` and Dominion checklist
  creatures `74759..74766`. The fallback final-departure terminals and checklist
  actors are also emitted with `HasInteractionPrereq`, matching the earlier
  mine-interaction fix and preventing dynamically spawned simple-collidable
  objectives from appearing without a usable client interaction affordance.
- While fixing that path, dynamic creature-id initialization for simple and
  simple-collidable entities was brought into line with DB-model initialization:
  both now load their entity scripts. This keeps fallback tutorial mines and
  other dynamically spawned simple actors on the same script path as imported
  world entities.
- This keeps terminal destination selection in the already mapped
  `RecordStarterTutorialDepartureTerminal` path, while fixing the fallback
  checklist actors so the final quest can advance all eight retail checklist
  bits instead of repeatedly setting bit `0`.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed with
  `348/348` tests, and `dotnet build
  Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj --no-restore
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed with `0`
  warnings and `0` errors.

Client unresolved opcode diagnostic-model follow-up:

- No new native labels were added. This pass closes the current client
  enum-only queue using the already recorded opcode comments and coverage
  inventory payload evidence: fixed raw byte payloads for `Client0x003D`,
  `Client0x00ED`, `Client0x0142`, `Client0x0760`, `Client0x0762`, and
  `Client0x07B6`; single scalar payloads for `Client0x00C8`, `Client0x011B`,
  `Client0x011D`, `Client0x012D`, `Client0x0550`, `Client0x062A`,
  `Client0x0634`, `Client0x0701`, `Client0x07E3`, and `Client0x0928`; and one
  wide-string payload for `Client0x063E`.
- NexusForever now has conservative receive models and diagnostic-only
  WorldServer handlers for all 17 unresolved client opcodes. The handlers log
  payload length or scalar value only; they intentionally do not mutate player,
  account, housing, matching, realm, or gameplay state while the opcode
  semantics remain unmapped.
- This pass resolves a source/coverage mismatch where the findings history had
  previously described a closed client opcode queue but the current source still
  exposed 17 client enum-only rows. The remaining missing-opcode work is now on
  the server-output side.
- Verification: `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` and
  `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` both passed with `0`
  warnings and `0` errors. `Get-DecompCoverageSnapshot.ps1` now reports client
  opcode coverage as `347` implemented, `0` partial, and `0` missing, with no
  client missing-model or missing-handler queue. The existing Rider's Reef
  entity/script surface was rechecked with `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` (`348/348` passed)
  and `dotnet build
  Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj --no-restore
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` (`0` warnings,
  `0` errors).

Server unresolved output opcode structural-model follow-up:

- No new native labels were added. This pass closes the current server
  enum-only queue using the opcode coverage inventory and existing opcode
  comments as structural evidence only: fixed-size raw byte payloads for the
  unresolved server outputs, plus the two non-byte-aligned shapes already
  recorded by the inventory (`18`-bit scalar and `uint32` plus `bool` flag).
- NexusForever now has conservative `IWritable` models for all 120 previously
  missing server-output opcodes. The unresolved models validate exact payload
  length for fixed-size packets and write only caller-supplied or zero-filled
  structural bytes; they intentionally do not introduce gameplay, account,
  realm, or session behavior while the retail semantics remain unmapped.
- This closes the source/coverage surface for server output opcodes:
  `Get-DecompCoverageSnapshot.ps1` now reports server opcode coverage as `692`
  implemented, `0` partial, and `0` missing, with no server missing-model queue
  and no placeholder-model queue. The `Server0xNNNN` class names remain
  unresolved structural names and should be decoded/renamed only after
  per-opcode client-reader evidence is mapped.
- Verification: `dotnet build
  Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` and
  `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` both passed with `0`
  warnings and `0` errors.

Missing-feature opcode workboard follow-up:

- `Decomp/Analysis/MISSING_FEATURE_MATRIX.md` is now the canonical
  system-by-system restoration matrix for the next opcode/decode passes. It
  separates structural opcode coverage from semantic feature support, lists the
  current high-value missing systems, and groups all `120` structural
  `Server0xNNNN` models plus all `17` diagnostic `Client0xNNNN` receive models
  into likely feature clusters based on neighboring named opcodes and existing
  findings.
- The matrix is intentionally a planning and execution workboard, not a rename
  authority. Rows marked by opcode neighborhood still need client reader/writer
  evidence, sniff/runtime corroboration, or table-backed proof before source
  names or server behavior are widened.

Fortune conservative session follow-up:

- No new native labels were added. This pass implements the `F-031` Fortune
  row from the missing-feature matrix using the existing modeled packet family
  and recorded packet comments: zero-byte notify/start client messages,
  one-uint card flip requests, `ServerFortuneRewards`, `ServerFortuneCards`,
  `ServerFortuneCardUpdate`, and `ServerFortuneReset`.
- WorldServer now keeps a conservative in-memory Fortune session per account.
  `ClientFortuneNotifyGame` and `ClientFortuneNotifyStorefront` send empty
  reward data plus current or reset card status; `ClientFortuneStart` creates a
  three-card empty session; and `ClientFortuneFlipCard` marks valid cards as
  flipped with `ServerFortuneCardUpdate`. Invalid flips or flips without a
  started session send `ServerFortuneReset` with the existing click-empty value
  `3`.
- Reward account items, money/currency payouts, eligibility/cost checks,
  storefront/game coordination, and durable retail resume state remain blocked
  until native reward-generation or sniff evidence maps the server-side policy.
- Verification: `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\fortune-world\`
  passed with `0` warnings and `0` errors. The focused Fortune test slice
  passed `5/5` tests with isolated output after a transient normal-output
  `WorldServer.obj` file lock, and the full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo` run passed `353/353` tests.

Challenge choice compatibility follow-up:

- No new native labels were added. This pass implements the non-mutating
  compatibility boundary for the `F-033` challenge row using the existing
  modeled `ClientChallengeChoice` packet (`ChallengeId`, `Choice`, and
  diagnostic-only `Unused`) and `ServerChallengeResult` packet.
- `ClientChallengeChoiceHandler` now keeps the existing diagnostic log and
  replies with `ServerChallengeResult.GenericFail` for the requested challenge
  id. This gives the client a deterministic result without activating,
  abandoning, accepting shared challenges, mutating objective state, awarding
  rewards, or clearing local challenge UI as if the request had succeeded.
- Active challenge lifecycle, shared challenge invitations, accept/decline
  semantics, cooldown/tier progression, `ServerChallengeUpdate` field
  population, `Client0x00C8`, and `QuestObjectiveType.CompleteChallenge`
  integration remain blocked until the accept/decline/update sequence is
  decoded from stronger client-reader or runtime evidence.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter ChallengeChoiceHandlerTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\challenge-choice-tests\`
  passed `1/1` focused test. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\challenge-choice-all-tests\`
  run passed `354/354` tests. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\challenge-choice-world\`
  passed with `0` warnings and `0` errors after an earlier parallel run hit a
  transient normal-`obj` compiler file lock.

Galactic Archive interact-unlock follow-up:

- No new native labels were added. This pass implements the creature interaction
  hook for the `F-034` datacube/Galactic Archive row using the existing
  `Creature2Entry.ArchiveArticleIdInteractUnlock` table field and the already
  modeled `IGalacticArchiveManager.UnlockArticle` path.
- `SimpleEntity.OnActivate` and `SimpleEntity.OnActivateCast` now request a
  Galactic Archive article unlock when `ArchiveArticleIdInteractUnlock` is
  populated. The call uses `grantRewards: false`, matching the existing
  `ClientGalacticArchiveUnlock` trust boundary, so creature interaction can
  reveal article state without speculatively awarding titles or broader archive
  rewards.
- ArchiveLink parent/child authorization, full journal/datacube progression,
  path-mission-backed rule unlock parity, and wider Codex UX behavior remain
  blocked until stronger client-reader, table-chain, or runtime evidence maps
  those semantics.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter SimpleEntityArchiveUnlockTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\simple-archive-tests\`
  passed `2/2` focused tests. `dotnet build
  Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -m:1
  -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\simple-archive-game\`
  passed with `0` warnings and `0` errors. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\simple-archive-all-tests\`
  run passed `356/356` tests.

Support submission boundary follow-up:

- No new native labels were added. This pass implements the service boundary
  requested by the `F-028` support/report/survey row without changing packet
  semantics or introducing a speculative moderation workflow.
- Support report, ticket, bug, suggestion, and customer-survey handlers now
  depend on `ISupportSubmissionStore`. The default `FileSupportSubmissionStore`
  preserves the existing daily JSONL capture under `support-submissions`, while
  tests and future database-backed support cases can replace the store without
  touching packet handlers.
- `ClientSupportTicketHandler` still validates the submitted language and sends
  `ServerSupportTicketResult.Success` based on whether the store accepted the
  ticket. Invalid language values short-circuit with failure and do not create
  a support submission.
- Durable case status/readback semantics, moderation workflow, administrative
  tooling, and the nearby unresolved support/realm result opcodes remain
  blocked until client reader/runtime evidence maps those flows.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter SupportTicketHandlerTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\support-ticket-tests\`
  passed `3/3` focused tests. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\support-boundary-world\`
  passed with `0` warnings and `0` errors. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\support-boundary-all-tests\`
  run passed `359/359` tests.

Leaderboard provider boundary follow-up:

- No new native labels were added. This pass implements the backing-service
  boundary for the `F-032` leaderboard row without adding speculative ranking
  storage or season/category semantics.
- `ClientLeaderboardPveRequestHandler` and `ClientLeaderboardPvpRequestHandler`
  now delegate response construction to `ILeaderboardProvider`. The registered
  default `EmptyLeaderboardProvider` preserves the existing deterministic
  compatibility behavior: PvE echoes leaderboard type, matching game map, and
  prime level with no player rows; PvP echoes leaderboard type with no team
  rows.
- Real leaderboard aggregation, personal placement, season windows,
  category/filter rules, and nonzero `NextUpdateTime` semantics remain blocked
  until PvE and PvP client reader paths or runtime captures map those fields.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter LeaderboardProviderTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\leaderboard-provider-tests\`
  passed `4/4` focused tests. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\leaderboard-provider-world\`
  passed with `0` warnings and `0` errors. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\leaderboard-provider-all-tests\`
  run passed `363/363` tests.

Option persistence follow-up:

- No new native labels were added. This pass implements the durable character
  storage step for the `F-027` options/combat-log preference row using the
  already mapped `ClientCombatOptions`, `ClientOptions`,
  `ClientCombatLogDisableOthers`, and `ClientCombatLogDisables` request
  shapes.
- `Database.Character` now maps `castingOptions`,
  `sharedChallengeEnabled`, `disableOtherPlayersCombatLogs`, and
  `combatLogDisableFlags` on the `character` table. `Player` loads those values
  from `CharacterModel`, marks them dirty when option handlers update the
  mapped preferences, and saves the four option columns with the existing
  character save path.
- The server-side runtime already suppresses outbound `ServerCombatLog` packets
  through `WorldEntity.ShouldSuppressCombatLogForPlayer`; this pass makes the
  preference source durable rather than session-only. It does not add new
  option types, account-level option splitting, or speculative option result
  packets.
- Remaining bounded uncertainty: exact client-facing option initialisation or
  readback packets, account-vs-character ownership for broader options, and the
  nearby unresolved item/options server opcode cluster remain blocked until
  stronger client-reader or runtime evidence maps those flows.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter OptionPersistenceTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\option-persistence-tests\`
  passed `2/2` focused tests. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\option-persistence-world\`
  passed with `0` warnings and `0` errors. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\option-persistence-all-tests\`
  run passed `365/365` tests.

Zone completion exploration-only reward follow-up:

- No new native labels were added. This pass implements a narrow `F-036`
  reward boundary using the existing `ZoneCompletion` table model, the existing
  `ZoneMapManager` map-complete detection, and the already wired
  `AchievementType.MapComplete` call.
- `ZoneCompletionRewardResolver` now resolves title rewards only for
  exploration-only rows: episode quest, task quest, challenge, datacube, tale,
  and journal counts must all be zero, the reward title id must fit in
  `ushort`, and all matching rows for the map zone must collapse to one
  distinct title id. Distinct title conflicts are treated as blocked because
  `ZoneCompletionFactionEnum` and path/faction/category semantics are not yet
  mapped.
- When a zone map becomes fully explored, `ZoneMapManager` still grants
  `AchievementType.MapComplete` and now grants only those unambiguous
  exploration-only title rewards. Full zone-completion payout, non-title
  rewards, faction/path-specific rows, and quest/challenge/datacube/journal
  total integration remain blocked.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter ZoneCompletionRewardResolverTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\zone-completion-tests\`
  passed `3/3` focused tests. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\zone-completion-world\`
  passed with `0` warnings and `0` errors. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\zone-completion-all-tests\`
  run passed `368/368` tests.

Mail transaction boundary follow-up:

- No new native labels were added. This pass implements the test gate requested
  by the `F-013` mail row before widening live settlement behavior.
- Focused `MailItem` transaction tests now pin the existing persistence
  boundaries for cash collection, returned mail, and attachment deletion:
  `PayOrTakeCash()` saves `HasPaidOrCollectedCurrency` and
  `MailFlag.NotReturnable`; `ReturnMail()` saves the recipient, returned
  subject, and not-returnable flag; and `AttachmentDelete()` enqueues the
  removed attachment for the deleted-attachment save path without leaving it in
  the active attachment list.
- No live mail settlement behavior changed. COD sender payment, marketplace
  settlement, delete-result state, expiration behavior, and broader atomicity
  across inventory/currency/mail writes remain blocked until delete/return
  result semantics and settlement flows are mapped with stronger client-reader,
  sniff, or runtime evidence.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter MailItemTransactionTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\mail-transaction-tests\`
  passed `3/3` focused tests. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\mail-transaction-all-tests\`
  run passed `371/371` tests.

Loot roll/master request boundary follow-up:

- No new native labels were added. This pass adds handler-level verification for
  the `F-014` loot row using the already modeled `ClientLootRollAction` and
  `ClientLootAssignMaster` request packets and the existing
  `IGlobalLootManager` roll/master-loot runtime boundary.
- `ClientLootRollActionHandler` is now covered for forwarding decoded
  owner-unit id, loot-unit id, and `LootRollAction` to `RollLoot`.
  `ClientLootAssignMasterHandler` is covered for forwarding owner-unit id,
  loot-unit id, and the converted assignee `Identity` to `AssignMasterLoot`.
  This pins the request-to-runtime handoff without adding new loot side
  effects.
- Follow-up reader evidence maps `0x08A0` to `ServerLootItemUpdate`: the
  WildStar64 registration at `1400795d4..140079600` uses reader `1400a4920`
  and object size `0x48`, and `ServerLootItemUpdate_ReadPayload` reads the
  same `LootItem` shape already used inside `ServerLootNotify`/`Grant`.
  `0x08A8` is not loot-shaped; the registration at
  `140075530..140075554` uses reader `140098460` and object size `0x14`, and
  `ServerEntityVisualInfoUpdate_ReadPayload` reads unit id, 18-bit Creature2
  id, 17-bit display info, and two trailing flags.
- Bindcheck/BOP reader evidence maps `ServerLootBindOnPickup` (`0x011C`) to
  `ServerTwoUInt32_ReadPayload` (`14007a040`) with object size `0x8`.
  `Loot_PrepareAndDispatchBindcheck` (`14039cee0`) keys the local loot tree
  from the second uint at `param_2 + 4` before dispatching `LootBindcheck`;
  the first uint is read but remains unconsumed in this mapped path.
- The bindcheck follow-up narrowed the confirmation path without proving a new
  opcode. `Loot_DispatchBindcheckEvent` (`140430e00`) serializes the cached
  loot row into an `itemDrop` payload and raises only the named
  `LootBindcheck` event. Direct and packed send-helper traces for adjacent
  `Client0x011B`/`Client0x011D` found no BOP-confirm send site, while the
  mapped `0x014F ClientLootItem` request/collect senders remain the only
  proven client loot-action outputs. Treat BOP confirmation as a live-capture
  blocker: prove whether accepting the prompt sends another `ClientLootItem`
  collect request before adding server-side pending-confirmation state.
- Loot feedback consumer evidence maps `Loot_HandleLootGrant` (`1403db050`)
  to `ServerLootGrant`: it dispatches `LootTakenBy`, removes the local loot
  row, and runs grant/destroy follow-up visuals. `Loot_HandleLootNotification`
  (`1403db1f0`) handles `ServerLootNotification` by ignoring local-looter
  notifications, resolving the row by `LootUnitId`, and dispatching
  `ChannelUpdate_Loot` for remote-looter feedback.
- The full loot-notify consumer is now labeled as `Loot_HandleLootNotify`
  (`1403daf90`). It calls `Loot_IngestServerLootNotify` (`1405fe2f0`), which
  copies each 0x48-byte `LootItem` row into the client loot tree via
  `Loot_FindOrCreateLootItemRow` (`1405ff6a0`). The copied row includes the
  Lua-facing `bCanLoot` slot at payload `+0x4c`, so the normal
  `ServerLootNotify`/`ServerLootItemUpdate` row paths already carry `CanLoot`
  state without the standalone scalar opcode.
- `ServerLootNotify.ParentUnitId` is now mapped to a client-side loot visual
  source field rather than an arbitrary spare dword. `Loot_HandleLootNotify`
  stores packet field `[1]` at owner entity `+0x36a8`; the only non-init reader
  found at `14045f080` resolves that stored entity id and uses the resolved
  entity/model radius while preparing loot visual placement. Current
  NexusForever still mirrors `OwnerUnitId` because `LootInstance` has no
  distinct parent/source entity for spawned loot visuals.
- The remaining roll feedback consumers are labeled:
  `Loot_HandleLootRollSelection` (`1403db510`) dispatches
  `LootRollSelected`/`LootRollPassed` feedback and removes the local roll row
  when the actor identity matches the local player, while
  `Loot_HandleLootWinner` (`1403db610`) dispatches roll rows, all-passed,
  assigned, or won feedback and removes the roll row through
  `Loot_RemoveLootItemFromUiTrees` (`1405ff370`).
- `ServerLootCanLoot` remains blocked as a standalone opcode. The direct
  opcode scan for `0x089F` only found the packet reader registration, the
  `0x7d90` loot-service scan found no scalar consumer, and the neighboring
  loot-service helper cluster did not expose a single-loot-id path that flips
  the copied `bCanLoot` field. Do not emit this opcode until a scalar consumer
  or live capture proves its timing and side effects.
- Bind-on-pickup confirmation policy, exact roll/master eligibility rules,
  parent source tracking, master-loot UI parity, `ServerLootCanLoot`
  consumer/timing, and the two compact visual-info flag meanings remain
  blocked until client consumer or runtime evidence maps those state
  transitions.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter LootRequestHandlerTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\loot-request-handler-tests\`
  passed `2/2` focused tests. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\loot-request-all-tests\`
  run passed `373/373` tests.

Steam achievement diagnostic boundary follow-up:

- No new native labels were added. This pass pins the `F-035` Steam achievement
  ingest boundary using the existing `ClientSteamAchievements` model and
  diagnostic `ClientSteamAchievementsHandler`.
- The packet test verifies the current wire shape as `SteamGameId`, byte
  length, and ASCII achievement payload. The handler test verifies that parsed
  Steam achievement data is accepted without sending achievement packets or
  mutating `ICharacterAchievementManager` progress. This keeps client-supplied
  Steam data diagnostic-only until payload grammar and achievement-id
  correlation are mapped.
- Full trigger coverage, Steam achievement ingest policy, exact realm-first
  broadcast semantics, remaining updater parity, and UI edge cases remain
  blocked until stronger payload, table, or runtime evidence maps those flows.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter ClientSteamAchievementsTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\steam-achievement-tests\`
  passed `2/2` focused tests. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\steam-achievement-all-tests\`
  run passed `375/375` tests.

Friendship social-option boundary follow-up:

- No new native labels were added. This pass pins the transient social-option
  behavior already present in the `F-012` friendship/chat surface without
  adding durable account settings or changing chat delivery.
- `ClientFriendshipSetAutoResponseMessageHandler` is now covered for storing
  parsed wide-string away and busy auto-response messages on the active player.
  `ClientFriendshipIgnoreStrangersStateHandler` is covered for echoing the
  client flag as `ServerFriendshipIgnoreStrangersState.Flags`: `2` when
  ignoring stranger invites and `0` when clearing the state.
- Persistent social options, exact auto-response delivery/readback semantics,
  entitlement checks, ICComm persistence, throttling, leave/logout cleanup, and
  chat auxiliary packets remain blocked until client-reader or runtime evidence
  maps those flows.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter FriendshipSocialOptionTests -m:1 -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\friendship-social-option-tests\`
  passed `3/3` focused tests. The full
  `Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\friendship-social-option-all-tests\`
  run passed `378/378` tests.

PvP duel lifecycle boundary follow-up:

- No new native labels were added. This pass pins the `F-015` transient duel
  lifecycle already implemented by `DuelManager`; no live PvP or duel runtime
  behavior changed.
- Focused tests now cover the mapped challenge flow: `Initiate()` sends
  `ServerDuelChallenge`, `Accept()` sends `ServerDuelCountdown`, the manager
  only enters active duel state after the three-second countdown, and
  `ServerDuelStart` is emitted to both participants. Pending challenges are
  also covered for thirty-second timeout cancellation through
  `ServerDuelResult(DuelCancelled)`.
- Active defeat is covered for `ServerDuelResult(Defeated)` and the current
  achievement boundary: both participants receive `DuelParticipate`, while the
  winner also receives `DuelWin`.
- Observer broadcasts, leash/warning state, PvP cooldown persistence,
  forced-map PvP, rewards/stats, and exact cancel/result parity remain blocked
  until client-reader, sniff, or manual runtime evidence maps those flows.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter
  FullyQualifiedName~NexusForever.Game.Tests.Pvp.DuelManagerTests -v minimal
  --nologo` passed `3/3` focused tests.

Crafting fixed-recipe boundary follow-up:

- No new native labels were added. This pass pins the `F-008` fixed-recipe
  crafting boundary already present in `ClientCraftingSimpleCraftHandler` and
  `CraftingCraftRequestHelper`; no live crafting behavior changed.
- Focused tests now cover a direct-output simple craft with an active
  tradeskill, an unambiguous `TradeskillSchematic2` row, and a mapped satchel
  material. The verified path removes the required `TradeskillMaterial` amount
  from `ISupplySatchelManager`, creates the direct output item with
  `ItemUpdateReason.Crafting`, grants `CraftItem` and `CraftItemChecklist`
  achievement updates, applies mapped crafting XP, and emits
  `ServerCraftingFinish` with `Pass = true`, the crafted schematic id, crafted
  item id, `CraftingDiscovery.Success`, and earned XP.
- The missing-material path is covered separately: when the required material
  is absent, the handler emits a failed `ServerCraftingFinish` and does not
  remove satchel material, create output, or update achievements.
- Discovery rolls, station constraints, complex/random output parity, broader
  material-source precision, rune item data, and `ServerTradeskillSigilResult`
  meanings remain blocked until client-reader, table, or runtime evidence maps
  those flows.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter
  FullyQualifiedName~NexusForever.Game.Tests.Crafting.CraftingSimpleCraftHandlerTests
  -v minimal --nologo` passed `2/2` focused tests.

Marketplace, pending item, transport, reward-property, and matching boundary follow-up:

- No new native labels were added. This pass implements narrow `F-005`,
  `F-006`, `F-007`, `F-009`, and `F-010` boundaries from already mapped or
  locally verified server surfaces.
- `F-005`: focused marketplace tests now pin the current transient auction path:
  `ClientAuctionSellOrderSubmit` removes the posted inventory item and returns
  `ServerAuctionPostResult`; `ClientAuctionsByFilterRequest` can find the
  posted listing by item id; `ClientAuctionBuyOrderSubmit` buyout debits buyer
  credits, transfers the item into buyer inventory, sends `ServerAuctionWon`,
  and returns `ServerAuctionBidResult.Ok`. Durable auction/commodity storage,
  offline settlement, CREDD, and mail settlement remain blocked on result/update
  packet and settlement evidence.
- `F-006`: the gift-to-account pending-group path now rejects the mapped
  `ReservedZero` field when nonzero before calling
  `GiftPendingItemGroupToAccount`, refreshes pending items, and returns a
  conservative `GiftItem/GenericFail`. Focused tests also pin the already
  implemented online account-gift happy path. Offline delivery, coupon policy,
  cooldown mutation, unsupported offer effects, and exact purchase-result UI
  remain blocked by the unresolved account/store output cluster.
- `F-007` and `F-009`: protocol tests now pin the known
  `ServerRewardPropertySet` modifier encodings and the already mapped transport
  packet shapes for `ClientRapidTransport`, `ClientFlightPathPurchase`,
  `ServerFlightPathUpdate`, and `ServerVehiclePassengerSelf`. These are
  boundary tests only; reward schedules, entry-state semantics, service-token
  bypass, taxi route lifecycle, and passenger/vehicle semantics remain blocked.
- `F-010`: `MatchingQueueValidator` now iterates
  `IMatchingCharacterQueue.MatchingQueueProposal` when enforcing the solo/group
  queue exclusivity rule. The focused test covers an existing solo queue plus an
  incoming party queue returning `CannotQueueSoloAndGroup`; group output
  packets, queue status timing, replacement/vote flows, and matching
  `Client0x062A/0634` remain decode-blocked.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter
  "FullyQualifiedName~AccountItemHandlerTests|FullyQualifiedName~AccountInventoryPendingGroupTests|FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~TransportPacketShapeTests|FullyQualifiedName~RewardPropertyProtocolTests|FullyQualifiedName~MatchingQueueValidatorTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed `16/16`
  focused tests.

Crafting/guild/social-diagnostic/CC protocol boundary follow-up:

- No new native labels were added. This pass adds test-only coverage for
  protocol boundaries that row workers identified as safe to pin while keeping
  their runtime behavior blocked.
- `F-008`: `CraftingPacketShapeTests` now cover the mapped request layouts for
  simple craft, complex craft, craft-item, auto-craft, and additive packets,
  plus the current `ServerCraftingFinish`, `ServerCraftingCurrentCraft`, and
  `ServerTradeskillSigilResult` output shapes. The complex-craft test writes
  the packed `CraftStats` payload directly because native evidence currently
  proves a 64-bit field but does not prove any broader server-side crafting
  stat semantics.
- `F-011`: `WarPartyBossTokenProtocolTests` now pin the
  `ClientWarPartyBossTokensRequest` guild identity and
  `ServerWarPartyBossTokens` identity/count/token-row encoding. Real token
  inventory, consumption, boss summon results, recruitment state, and guild bank
  or perk behavior remain blocked.
- `F-002/F-012`: `ClientDiagnosticPacketShapeTests` pins `Client0x0550` as a
  mapped uint32 diagnostic payload. Its owner is still only correlated between
  ICComm and spell-list neighbors, so the handler remains diagnostic-only.
- `F-018`: `CrowdControlPacketShapeTests` now pin the current stun update,
  knockdown break, stun direction, CC state set/remove, tether-unit, and
  spell-buff-remove packet shapes. Stack-group arbitration, stun breakout
  cadence, tether semantics, forced movement, and facing blend behavior remain
  blocked until stronger reader/sniff/runtime evidence maps those paths.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter
  "FullyQualifiedName~CraftingPacketShapeTests|FullyQualifiedName~WarPartyBossTokenProtocolTests|FullyQualifiedName~ClientDiagnosticPacketShapeTests|FullyQualifiedName~CrowdControlPacketShapeTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed `18/18`
  focused tests.

Entity/item/support/realm/Fortune protocol boundary follow-up:

- No new native labels were added. This pass keeps runtime behavior inside
  already mapped or compatibility-only surfaces while adding focused tests for
  row-agent recommended boundaries.
- `F-025`: `EntityCreatePacketTests` now includes a serialized
  `ServerEntityCreate` regression covering the mapped `WorldPlacement`
  `Type = 1` payload, default `UnknownStructureA8/C8` selectors, and final
  minimap/display/outfit tail fields. Remaining entity-create substructures,
  deferred action queues, CSI/current-target behavior, phase visibility, and
  threat/entity-stat packets remain blocked.
- `F-026`: `AccountItemHandlerTests` now pin `ClientItemGenericUnlockHandler`
  lifecycle boundaries for missing unlock set, missing unlock entry, all
  entries already unlocked, and partial unlock sets. The partial case consumes
  the item once and grants only missing entries. Broader item aux packets,
  costume/pet/generic-unlock deltas, and persistence parity remain blocked.
- `F-028`: `Survey.cs` now reads the mapped survey context/object field as a
  32-bit unsigned value instead of calling the 16-bit reader with a 32-bit
  width. `CustomerSurveyProtocolTests` cover all currently supported survey
  models and the submission-store boundary. Support case workflow, moderation
  state, stuck cooldown/result behavior, and survey admin tooling remain
  blocked.
- `F-030`: `ClientGetRealmTransferDestinationsHandler` now sends an empty
  `ServerTransferDestinationRealmList` compatibility response, and
  `RealmTransferProtocolTests` pin empty handler response plus one-row packet
  shape. Real realm transfer destinations/results, PTR copy state, pricing,
  eligibility, and admin result packets remain blocked.
- `F-031`: `FortunePacketShapeTests` now pin `ClientFortuneFlipCard`,
  `ServerFortuneCards`, `ServerFortuneCardUpdate`, `ServerFortuneRewards`, and
  `ServerFortuneReset` wire shapes. Reward payout, cost/eligibility,
  storefront synchronization, and durable resume semantics remain blocked.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore
  --filter
  "FullyQualifiedName~AccountItemHandlerTests|FullyQualifiedName~EntityCreatePacketTests|FullyQualifiedName~CustomerSurveyProtocolTests|FullyQualifiedName~RealmTransferProtocolTests|FullyQualifiedName~FortunePacketShapeTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed `59/59`
  focused tests.
- Full verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed `429/429`
  tests.

Account/store `Server0x0969` structural follow-up:

- `FUN_14006c290` registers account/store opcode `0x0969` with reader
  `140080710` and a `0x10` object size. `InspectCodeAddress.java` against
  `140080710` maps the reader as a 32-bit count followed by `count` rows of two
  32-bit fields. The same reader is also registered for the already named
  `ServerAccountEntitlements` opcode `0x0968`.
- `140080710` is now labelled `ServerUInt32PairArray_ReadPayload`. NexusForever
  originally kept the packet name as `Server0x0969` while the row semantics were
  unknown. A later account-consumer pass proves the rows are account item
  cooldown group and remaining-second pairs, and the model is now named
  `ServerAccountItemCooldowns`.
- Follow-up inspection of adjacent readers maps `140080810` as opcode `0x096E`
  with five 32-bit fields, one float, and a trailing 3-bit value, and
  `1400808d0` as opcode `0x0971` with one 3-bit value followed by one float.
  The later consumer pass maps these as `ServerDailyLoginUpdate` and
  `ServerAccountPrivilegeRestrictionUpdate`.
- Offline pending-group delivery, coupon policy, cooldown mutation, exact
  purchase-result UI, and the remaining `096A..0991` account/store opcodes stay
  blocked until their readers and client-facing state changes are mapped.

Account/store terminal cluster structural decode follow-up:

- The rest of the `F-006` terminal cluster now has field-level reader evidence
  applied as durable labels and re-exported in
  `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled.c`.
  `096A` is `uint32 + AccountInventoryItem`; `096B` is
  `uint32 + count + AccountInventoryItem rows`; `096C` is
  `uint32 + account inventory item id`; `0976` is one
  `PendingAccountItemGroup` row; `0977` and `0989` are empty;
  `0978` is a count plus rows of `uint32`, flag, `uint32`, two identities, and
  two `uint64` values; `097A` is a count plus rows of `uint64`, 14-bit value,
  and 7-bit value; `097B` is a 6-bit value; `097D` is wide string plus flag;
  `097E` is `uint64` plus flag; `0980` is `uint32`; `0986` is `uint64`;
  `098A` is a 5-bit value; `098C`, `098D`, and `0990` are flag plus 5-bit
  value; `098E` is a count plus store-shaped rows; `098F` matches the store
  currency-package row shape; and `0991` is flag, 5-bit value, eight wide
  strings, three floats, and one trailing wide string.
- `ServerStoreOffers_OfferItem_ReadPayload` proves the item row always carries a
  trailing `uint32` after the type-specific body. NexusForever now writes that
  trailing `Amount` for type `1` and type `2` offer item variants as well as
  type `0`.
- Focused coverage: `PacketPlaceholderNamingTests` now pins all newly decoded
  account/store shapes plus the `ServerStoreOffers.OfferItemData` trailing
  amount behavior. Verification used
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~PacketPlaceholderNamingTests" -m:1
  -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\account-store-packet-tests\`
  and passed `27/27` tests.
- Packets without mapped consumer semantics remain `Server0xNNNN` models with
  generic field names on purpose. Under the semantic completion bar, `F-006` is
  still blocked until each unresolved opcode has a native emit site, client
  UI/event consumer, packet name, state mutation, and live or fixture
  verification. Do not use these shapes to implement offline pending-group
  delivery, coupon policy, additional cooldown policy, purchase-result UI, or
  unsupported offer item effects until that semantic evidence exists.

Account/store `0x0969..0x0980` client-consumer semantics:

- `AccountInventory_HandleServer0966To0980` (`140003fb0`) routes the
  account-side cluster after payload readers run. The mapped consumer path now
  resolves several packets that were previously structural-only.
- `0x0969` is the initial account item cooldown list. The handler
  `AccountItemCooldownList_HandleServer0969` (`1400051c0`) consumes each
  uint32 pair as cooldown group id and remaining seconds, then stores an expiry
  timestamp in the same cache updated by `ServerAccountItemCooldownSet`
  (`0x0974`). NexusForever now names the packet
  `ServerAccountItemCooldowns` and sends the retail count-plus-pair list during
  initial cooldown sync while preserving `ServerAccountItemCooldownSet` for live
  single-cooldown mutations.
- `0x096E` is `ServerDailyLoginUpdate`. The consumer writes daily-login state,
  schedules a timer using the fifth uint32 delay, converts the float field into
  a FILETIME expiry in days, stores the 3-bit premium-key status, and dispatches
  `DailyLoginUpdate`. The Lua table builder exposes `nLoginDaysTotal`,
  `nRewardsAvailable`, `itemKey`, `ePremiumKeyStatus`, and
  `fSecondsUntilNextKey`.
- `0x0971` is `ServerAccountPrivilegeRestrictionUpdate`. The consumer stores a
  restriction expiry for indices `0..3`, converts the float day duration to
  seconds for the event, and dispatches `AccountPrivilegeRestrictionUpdate` with
  the active flag from `AccountPrivilegeRestriction_IsActive`.
- `0x0976`, `0x0977`, `0x097D`, and `0x097E` mutate the pending-account-item
  caches. `0x0976` appends one pending group row; `0x0977` clears grouped and
  ungrouped pending caches; `0x097D` removes a group by string key; and `0x097E`
  removes one ungrouped pending item by id. The add/delete paths dispatch
  `AccountPendingItemsUpdate` and `AccountItemUpdate`; the trailing flag fields
  on `097D/097E` are not consumed by the observed handlers.
- `0x0978` is `ServerCREDDOperationHistory`: when no CREDD order is pending,
  the consumer converts the decoded rows into `Game.Money` history objects and
  dispatches `CREDDOperationHistoryResults`.
- `0x097B` is `ServerCREDDRedeemResult`: the six-bit value is dispatched through
  `CREDDRedeemResult`.
- `0x0980` is `ServerWalletUpdate`: the consumer updates the account wallet
  value and dispatches `WalletUpdate` only when the value changed.
- `0x096A`, `0x096B`, and `0x096C` are account item cache add/list/remove
  messages. NexusForever now names these packets
  `ServerAccountItemCacheAdd`, `ServerAccountItemCacheListAppend`, and
  `ServerAccountItemCacheRemove`; the leading uint32 field remains `Unknown0`
  because the observed handlers do not consume it. `0x097A` is now named
  `ServerCREDDExchangeOrderCacheRows` for its internal CREDD exchange order
  cache refresh; its row field semantics remain blocked.
- Verification: focused packet, cooldown, and CREDD history tests now pass with
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~AccountItemCooldownTests|FullyQualifiedName~CREDDExchangeHandlerTests"
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-account-store\`
  (`37/37`).

Storefront `0x0987..0x0991` client-consumer semantics:

- `Storefront_HandleServer0987To0991` (`14044b630`) dispatches store result
  opcodes after their payload readers run. This provides semantic evidence for
  several previously generic store packets, but it is still client-consumer
  evidence, not server emit-site proof.
- `0x0989` is an empty `StoreCatalogUpdated` notification. The handler marks
  the store catalog dirty and dispatches `StoreCatalogUpdated`.
- `0x098A` is `StoreError`: a single 5-bit error value is dispatched through
  the client event `StoreError`.
- `CodeEnumStoreError` is registered by `Lua_RegisterGameEnumTables`
  (`1404f2860`) with the 22 mapped values `CatalogUnavailable`,
  `StoreDisabled`, `InvalidOffer`, `InvalidPrice`, `GenericFail`,
  `PurchasePending`, `PgWs_CartFraudFailure`, `PgWs_CartPaymentFailure`,
  `PgWs_InvalidCCExpirationDate`, `PgWs_InvalidCreditCardNumber`,
  `PgWs_CreditCardExpired`, `PgWs_CreditCardDeclined`,
  `PgWs_CreditFloorExceeded`, `PgWs_InventoryStatusFailure`,
  `PgWs_PaymentPostAuthFailure`, `PgWs_SubmitCartFailed`,
  `PurchaseVelocityLimit`, `MissingItemEntitlement`,
  `IneligibleGiftRecipient`, `CannotUseOffer`, `MissingEntitlement`, and
  `CannotGiftOffer`.
- `0x098C` and `0x098D` both dispatch `StorePurchaseOfferResult` with a success
  flag and a 5-bit value now correlated with
  `CodeEnumPurchaseResultDisplayType`: `Default`, `VIPSubscription`, and
  `NothingToClaim`. Their opcode variant distinction is still unresolved, so
  the NexusForever models stay `Server0x098C` and `Server0x098D` for now.
- `0x098E` replaces the client's purchase-history row cache and dispatches
  `StorePurchaseHistoryReady`.
- `0x098F` is registered with the same `uint32 + wide string + uint32 + float +
  uint32` shape used by store currency packages, but the mapped store consumer
  switch falls through without dispatching a client event for `0x098F`.
- `0x0990` dispatches `StoreCompleteOrderVirtualCurrencyPackageResult` with the
  flag and 5-bit result value.
- `0x0991` dispatches `StorePurchaseVirtualCurrencyPackageResult` with the
  decoded flag, 5-bit value, string, and float fields. The model is now named
  for the event, but individual field names remain conservative until the
  purchase flow or Lua consumer proves their exact meanings.
- `Storefront_SendClientPurchaseVirtualCurrencyPackage` (`1404f1d50`) sends
  opcode `0x082E` with one package-id byte and is registered in
  `FUN_14006c290` (`1400781c0`). NexusForever now names
  `ClientStorefrontPurchaseVirtualCurrencyPackage` and grants emulated NCoin/
  Omnibit packages from a hardcoded catalog before emitting `0x0991` and
  `0x0990`. Real-money billing remains out of scope.

One-hundred-twenty-fourth storefront error/result semantics pass:

- `Storefront_SendClientPurchaseCharacterOffer` (`140450720`) sends opcode
  `0x082A` from the normal StorefrontLib purchase route.
  `Storefront_SendClientPurchaseAccountOffer` (`1404507e0`) sends opcode
  `0x0828` from the recipient/account purchase route. These senders confirm
  that NexusForever's character and account purchase handlers own the mapped
  `StoreError` response surface.
- NexusForever now exposes the retail `StoreError` and
  `PurchaseResultDisplayType` enum values in `Game.Static.Storefront`.
  `ServerStoreError` has a semantic `Error` property, while `Server0x098C` and
  `Server0x098D` expose the shared `IsSuccess` and `DisplayType` aliases
  without renaming the unresolved opcode variants.
- Storefront purchase rejection paths now emit `ServerStoreError` (`0x098A`)
  instead of unrelated generic-error packets. The current mappings are
  conservative: unknown offers return `InvalidOffer`; invalid or missing price
  data returns `InvalidPrice`; recipient lookup failures return
  `IneligibleGiftRecipient`; unsupported offer/account-item placement and
  insufficient account currency return `CannotUseOffer`; and target/session
  guard failures return `GenericFail`.
- Blocker: no opcode-specific sender or UI branch has been found that separates
  `0x098C` from `0x098D`, and success-result emission still lacks server
  evidence. Purchase success paths therefore continue to deliver account items
  without inventing a synthetic `StorePurchaseOfferResult` packet.
- Immediate scans for the store purchase-result variants keep the blocker in
  place: `0x098D` and `0x098F` each appear only in the `FUN_14006c290`
  registration table, while `0x098C` has one opcode-registration hit and other
  unrelated structure-offset hits. `Storefront_HandleServer0987To0991`
  dispatches `0x098C` and `0x098D` through the same
  `StorePurchaseOfferResult` path, and falls through for `0x098F`; no separate
  UI branch or variant-specific state transition has been proven.
- `0x0986` follow-up: `FindImmediateInstructions.java` for immediate `0x986`
  finds only the world opcode registration in `FUN_14006c290`
  (`140078152`), where it is registered as an 8-byte server payload using
  `ServerUInt64_ReadPayload`. `Storefront_HandleServer0987To0991`
  (`14044b630`) explicitly gates the store result dispatcher to
  `0x0987..0x0991`, so `0x0986` has no mapped store event consumer yet and
  remains blocked as a raw `uint64` packet.

One-hundred-twenty-eighth pending/store negative-evidence pass:

- `AccountPendingItemGroupCache_LookupByPendingItemId` (`140007810`) proves grouped
  pending groups are indexed by the wire `Id` u64 at `+0x00`, not the trailing u64 at
  `+0x10` (`Unknown2`). Gift UI (`AccountItemUi_GiftSelectedPendingItemGroup`,
  `Lua_AccountItemLib_GiftPendingItemGroupToAccount`) reads `TargetAccountId` from cache
  `+0x38` only. `Unknown2` stays blocked; NF should keep emitting zero.
- `Storefront_ApplyServerStoreOffers` (`14044b750`) still does not consume catalog-offer
  `+0x28/+0x30`. `Storefront_HandleStorePurchaseHistoryReady` (`14044c540`, opcode `0x098E`)
  uses different row layout offsets; do not conflate with `ServerStoreOffers.Offer.Unknown6`.
- `FindCallsToTarget` shows only `0976` and `0979` call
  `AccountPendingItemGroupCache_InsertFromPayload` (`140005bf0`).

One-hundred-twenty-seventh storefront account purchase and pending gift emit pass:

- `Storefront_PurchaseAccount_WritePayload` (`140080a20`) appends a second u32 at struct
  `+0x30` after the shared character purchase body, using the same Lua `PurchaseOffer`
  arg 4 binding as `PurchaseExtensionId`. NexusForever names this wire slot
  `AccountPurchaseExtensionId` on `ClientStorefrontPurchaseAccount` (handlers still
  ignore it). `AccountItem_SendClientGiftPendingItemGroupToAccount` (`140006e50`)
  confirms pending-group `TargetAccountId` is the u64 consumed for account gifts;
  emitters now populate `TargetAccountId` when delivering pending groups to a
  recipient account.
- `PendingAccountItemGroup.Unknown2` (`+0x10`) remains blocked: cached at
  `AccountPendingItemGroupCache_InsertFromPayload` (`140005bf0`) but no claim/gift
  UI consumer was found for that slot.

One-hundred-twenty-sixth pending account item group field semantics pass:

- `PendingAccountItemGroup_ReadPayload` (`1400a9b20`) and
  `AccountPendingItemGroupCache_InsertFromPayload` (`140005bf0`) bound the 0x60-byte
  pending-group row. NexusForever now names `SenderAccountId`, `TargetAccountId`, and
  wire `HasTargetPlayerIdentity` (u64) on `ServerAccountItemsPending.PendingAccountItemGroup`.
  `Unknown2` at `+0x10` remains blocked. Emitters populate sender account id and the
  target-player flag from existing NF pending-item state.
- `Storefront_ApplyServerStoreOffers` (`14044b750`) copies offer id, strings, headline
  price dword, and nested currency/item rows into the client cache but does not reference
  parsed offer `+0x28/+0x30` in the inspected path; `Unknown6`/`Unknown7` stay blocked.

One-hundred-twenty-fifth storefront purchase field semantics pass:

- `Storefront_PurchaseCharacter_WritePayload` (`1400acf80`) and
  `StorefrontLib_PurchaseOffer` (`1404f1150`) correlate the shared `0x082A` /
  `0x0828` purchase payload fields: 5-bit `Game.Money` slot at `+0x14`,
  32-bit money amount float bits, 14-bit account currency type id, Lua purchase
  arg 3, target identity, and Lua purchase arg 4. NexusForever now names these
  `PaymentCurrencySlot`, `PurchaseMoneyAmountBits`, `PurchaseOptionId`, and
  `PurchaseExtensionId` on `ClientStorefrontPurchaseCharacter` and
  `ClientStorefrontPurchaseAccount`. NF handlers still only validate
  `OfferId` and `CurrencyId`.
- `ServerStoreOffers_Offer_ReadPayload` (`1400a0dc0`) reads int64 `+0x28` and
  8 bytes `+0x30` after `DisplayFlags`; retail `store_offer_item.field_6` is
  almost always `-1016071787`. These remain `Unknown6` / `Unknown7` until a
  named client consumer is proven. See `PLACEHOLDER_RENAME_TRACKER.md`.

One-hundred-twenty-third CREDD info/history response semantics pass:

- `CREDDOperationHistory_BuildLuaResults` (`14042c760`) builds the client
  `CREDDOperationHistoryResults` Lua event from the `0x0978` row cache. The row
  fields are now semantically bounded as `eOperation`, `bInitiator`,
  `monAmount`, `nLogAge`, and optional `nFriendId`. The client converts the
  packet age value through a minutes-to-time calculation before presenting
  `nLogAge`, so NexusForever exposes the decoded field as `LogAgeMinutes`
  rather than a final UI duration.
- `CREDDOperationHistory_ResizeRows` (`1400076e0`) resizes the operation-history
  cache to `count * 0x40`, matching the mapped `ServerCREDDOperationHistory`
  row reader. `ServerCREDDExchangeOrderCacheRows_ReadPayload` (`1400a0210`)
  reads compact `0x10`-byte rows from `0x097A`, and
  `CREDDExchangeInfo_AppendOrderCacheRows` (`140007760`) appends them into an
  account/CREDD cache. No mapped client event dispatch has been proven for
  `0x097A`, so it remains an internal cache refresh with blocked row field
  semantics.
- `CREDDExchangeInfo_BuildLuaResults` (`14042b9a0`) dispatches
  `CREDDExchangeInfoResults` and is tied to the client string reference at
  `140afc518`. The function builds empty-safe result tables and has evidence
  for buy/sell counts, price buckets, and owned order rows, but the non-empty
  server packet row writer and exact owned-order field semantics are not yet
  complete. NexusForever therefore names `0x026A` as
  `ServerCREDDExchangeInfoResults` and writes the mapped fixed `0x50` header:
  buy/sell counts, three buy and three sell `uint64` price buckets, owned-order
  count, trailing reserved fields. Owned-order row pointer data when count is
  non-zero remains blocked.
- `DailyLogin_SendClientClaimReward` (`1400070f0`) sends opcode `0x078F` with
  a zero-byte payload when claim preconditions pass.
- Runtime implementation:
  `ClientCREDDExchangeRequestInfo` now queues an empty
  `ServerCREDDExchangeInfoResults` snapshot before the successful
  `GetCREDDExchangeInfo` operation result. `ClientCREDDExchangeRequestHistory`
  now queues a `ServerCREDDOperationHistory` list before the same operation
  result. The list is empty without transient runtime activity, and includes
  mapped `eOperation`, `bInitiator`, `monAmount`, `nLogAge`, identity,
  counterparty, and optional friend-character id rows for transient CREDD
  submit, match-complete, and cancel events. This resolves non-empty history
  rows for the in-memory runtime boundary only; persistent CREDD orders,
  non-empty exchange-info price/owned-order rows, and offline settlement remain
  blocked until the non-empty packet semantics and storage model are resolved.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~CREDDExchangeHandlerTests|FullyQualifiedName~PacketPlaceholderNamingTests"
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\credd-handler-tests\`
  passed (`30/30`).

Housing visit-residence identity follow-up:

- `Housing_SendClientVisitResidenceIdentity` (`1405b2390`) is already labelled
  as the client path that sends opcode `0x052F` with a `TargetResidence`
  identity. NexusForever now routes `ClientHousingVisitResidence` through the
  same residence lookup, privacy gate, map-lock lookup, entrance rotation, and
  teleport behavior used by the broader `ClientHousingVisit` request.
- Focused tests cover non-residence-map rejection, unknown residence
  `Visit_Failed`, private residence `Visit_Private`, public residence teleport,
  and the packet's 14-bit realm plus 64-bit residence-id shape.
- Neighbor permissions, roommate persistence, community donation/placement,
  decor/plugin prerequisite parity, and the unresolved housing server-output
  cluster remain blocked until their client readers and server state
  transitions are mapped.

Housing community rename result correction:

- `ClientHousingCommunityRenameHandler` already computes retail-facing
  `HousingResult` values for leader permission, text validation, cost-table,
  and currency checks before mutating community state. The first compatibility
  response fix stopped hard-coding `Success`; the later WildStar64 opcode pass
  maps the post-rename result event as `ServerHousingCommunityRenameResult`
  (`0x078C`) and sends the computed result there so failed validation is visible
  to the client.
- Focused coverage pins the non-leader path returning
  `HousingResult.InvalidPermissions` without calling `RenameGuild`. Broader
  community rename costs, result strings, and success-side persistence are
  still bounded by the existing community handler and remain candidates for
  live smoke once community creation/placement is exercised.

Housing result enum semantic follow-up:

- `Lua_RegisterHousingLib` (`WildStar64.exe` `140737da0`) registers the
  `Game.Housing`/`HousingLib` Lua surface and the housing constant tables. Its
  `HousingResult_*` strings include the retail
  `HousingResult_Neighbor_Success` constant occupying the previously unnamed
  value `2`; NexusForever now exposes `HousingResult.Neighbor_Success` and
  focused packet coverage pins `ServerHousingResult` writing that value as the
  7-bit result field.
- `Houston64.exe` remains useful as a separate housing/client-tool context, but
  the current Houston export has no `HousingResult_*` enum strings. Treat
  WildStar64 as the authoritative runtime source for housing result semantics
  unless a future Houston-specific housing tool behavior is being mapped.

Housing contribution table default-load follow-up:

- `ClientHousingVendorListHandler` already derives plug vendor costs from
  `GameTableManager.HousingContributionInfo`, and WildStar64's
  `Housing_SendClientPlugRepair` evidence ties plug repair costs to
  `HousingContributionInfo` ids. Houston64, as a separate housing/client-tool
  context, now has the durable
  `ClientDB_RegisterHousingContributionInfo` label at `1400c3c80`; the selected
  decompile shows the `HousingContributionInfo` descriptor and
  `DB\HousingContributionInfo.tbl` registration.
- `GameTableManager.HousingContributionInfo` now carries `[GameData]`, so the
  default runtime table initialisation loads it before housing vendor-list cost
  generation. Focused contract coverage includes the default
  `HousingContributionInfo.tbl` filename.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~GameTableManagerGameDataContractTests" -m:1 -v minimal
  --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\housing-table-tests\`
  passed (`9/9`). `run_ghidra_analysis.ps1 -ExportOnly -Targets
  Houston64.exe -MaxDecompiledFunctions 990` applied the Houston label, and
  `Test-DecompileManifest.ps1 -FailOnMismatch` passed for Houston64.

Marketplace online seller settlement coverage follow-up:

- No runtime behavior changed. The transient auction order book already pays an
  online seller through `PlayerManager.Instance.GetPlayer(ownerCharacterId)` on
  buyout before transferring the item to the buyer. Focused marketplace
  coverage now registers the seller in the test `PlayerManager` and verifies the
  seller receives the accepted buyout credit amount while the buyer is debited
  and receives the item.
- Offline seller settlement, bidder/seller mail delivery, durable auction
  storage, commodity matching precision, and non-empty CREDD exchange
  persistence/history remain blocked on the existing marketplace/mail evidence
  gaps.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests"
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\marketplace-tests\`
  passed (`1/1`).

Crafting fixed-recipe count/catalyst boundary follow-up:

- No new native labels were added. The existing mapped `ClientCraftingCraftItem`
  writer (`1400a5a70`) sends the auto-craft prefix plus an 18-bit catalyst item
  id. Focused tests now pin the current fixed-recipe handler boundary for
  `SchematicCount > 1`: material and catalyst debits scale by count, output
  creation scales by count, achievements receive the crafted count, and earned
  crafting XP is forwarded in `ServerCraftingFinish`.
- Discovery rolls, station constraints, coordinate hot/cold math, complex or
  random output parity, durable rune item state, and sigil result meanings
  remain blocked on the previously recorded client-reader/table/runtime
  evidence gaps.

Challenge diagnostic `Client0x00C8` boundary follow-up:

- No runtime challenge behavior changed. `Client0x00C8` remains
  diagnostic-only, but its known 32-bit packet shape is now covered by a
  focused regression next to `Client0x0550`.
- Active challenge lifecycle, shared challenge accept/decline sequencing,
  `ServerChallengeUpdate` population, reward tiers, quest-objective mutation,
  and the exact `Client0x00C8` owner remain blocked until the challenge UI
  reader/sender flow is decoded.

Client diagnostic packet-shape completion follow-up:

- No runtime behavior changed. The full `F-002` diagnostic receive set now has
  focused packet-shape coverage for each conservative model:
  `Client0x003D`, `00C8`, `00ED`, `011B`, `011D`, `012D`, `0142`, `0550`,
  `062A`, `0634`, `063E`, `0701`, `0760`, `0762`, `07B6`, `07E3`, and `0928`.
- The tests pin the current decoded boundary only: fixed opaque payload lengths,
  scalar widths, and the one wide-string diagnostic payload. All handlers remain
  non-mutating because the client writer/sender owners and server response
  semantics are still unmapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~ClientDiagnosticPacketShapeTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed (`17/17`).

Mail pending-delivery promotion follow-up:

- No new native labels were added. This is a source-level correctness fix inside
  the existing mail delivery boundary: `MailManager.Update` now removes a ready
  delayed mail item from `pendingMail` when it promotes it into `availableMail`.
  Before this pass, the same pending item stayed in the pending list and could
  be added again on a later update tick.
- Focused coverage now verifies that a ready pending mail item is promoted once,
  emits one `ServerMailAvailable` packet, and leaves the pending list empty
  across a second update tick.
- Marketplace settlement, exact delete/return/result parity, expiration
  semantics, and broader attachment/cash atomicity remain evidence-blocked.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed (`4/4`).

Mail COD sender settlement follow-up:

- No new native labels were added. This is a source-level verification pass for
  an existing `F-013` mail behavior: `MailManager.MailPayCod` subtracts the COD
  amount from the recipient, marks the COD mail paid/not-returnable, queues an
  instant outgoing player mail back to the original sender, and returns
  `ServerMailResult` with `PayCashOnDelivery`/`Ok`.
- Focused coverage now verifies the settlement mail recipient/sender inversion,
  subject prefix, credit amount, instant delivery speed, non-COD cash payload,
  buyer debit, original mail state mutation, and result packet.
- Focused coverage also verifies the existing expiration sweep: available
  expired mail is removed, enqueued for delete, moved into the expired-save
  list, and announced with `ServerMailUnavailable`; pending expired mail is
  removed and enqueued for delete without a client unavailable notification.
- Marketplace/offline settlement, exact delete/return/result parity, exact
  expiration timing/result semantics, and broader attachment/cash atomicity
  remain evidence-blocked.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-mail\
  --filter "FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests"`
  passed (`7/7`).

Group harvest loot-rule state follow-up:

- No new native labels were added. Existing client evidence maps four loot-rule
  fields in `ClientGroupLootRulesChange` and `ServerGroupLootRulesChange`, and
  the group server already carries `HarvestRule` through its internal group
  messages. NexusForever now preserves that `HarvestRule` when converting
  internal groups into runtime `GroupLootState`, when ordering group state in
  `GroupStateManager.UpdateGroup`, and when removing a member from a cached
  group state.
- This does not implement harvesting distribution or eligibility behavior; it
  only stops dropping the already-published rule at the runtime state boundary.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~GroupStateManagerTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed (`3/3`).

Raid info runtime lock compatibility follow-up:

- `ClientRaidInfoRequest` is now explicitly covered for both empty state and
  active solo runtime instance locks. The handler looks up the player's solo
  `IMapLockCollection`, skips residence locks (`WorldId == 0`), and reports
  each active instance as a conservative `ServerRaidInfoResponse` row using the
  lock GUID's low 64 bits as the saved-instance id, the lock world id, a
  seven-day compatibility expiry, and prime level `0`.
- Real raid lock persistence, reset timing, prime-level rows, group/raid
  save-state integration, and the exact saved-instance id source remain blocked
  until instance save/restore semantics and the raid-info UI reader are mapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~ClientRaidInfoRequestHandlerTests|FullyQualifiedName~ClientMatchingQueueLeaveAsGroupHandlerTests|FullyQualifiedName~GroupStateManagerTests"
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\raid-matching-tests\`
  passed (`7/7`).

Matching leave-as-group queue boundary follow-up:

- No new native labels were added. The `ClientMatchingQueueLeaveAsGroup`
  handler now uses the existing `IGroupStateManager` snapshot to fan out the
  same `LeaveQueue` request to online group members after removing the requester
  from the selected match-type queue.
- Focused coverage pins both boundaries: grouped requests leave the queue for
  the requester and one online group member; non-grouped requests leave only the
  requester. This is still a queue-lifecycle compatibility behavior, not full
  retail matching parity.
- Role checks, queue status timing, replacement/vote flows, and the unresolved
  matching diagnostic receive opcodes remain blocked on the existing client
  reader/sender evidence gaps.
- Verification is included in the raid-info command above.

Matching role enforcer coverage follow-up:

- No runtime behavior changed. Focused coverage now pins the existing
  `MatchingRoleEnforcer` composition reducer: a flexible tank/DPS member is
  reduced to tank when the party already has one healer and three DPS, four
  single-role DPS members are rejected, and members with `Role.None` keep the
  proposal unsuccessful.
- This covers only the local queue proposal role reducer. Queue status timing,
  replacement/vote flows, exact role-selection lifecycle and client signaling,
  group member role-change packets, and saved-instance/raid integration remain
  blocked on the existing client reader/sender evidence gaps.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-matching-role\
  --filter "FullyQualifiedName~MatchingRoleEnforcerTests"` passed (`3/3`).

Group flag bridge follow-up:

- No group-server behavior changed. Focused coverage now pins the world-facing
  bridge for `ClientGroupFlagsChanged` and `ClientGroupSetRole`: the handlers
  publish the source character identity, target identity where present, group
  id, and requested group/member flags to the internal group boundary.
- The corresponding world internal handlers now have coverage for broadcasting
  `ServerGroupFlagsChanged` and `ServerGroupMemberFlagsChanged` to online group
  members while skipping members that are absent from the local player manager.
- Full member-flag effects, raw roster/detail/role-change packet payloads,
  queue status timing, replacement/vote flows, cross-realm data, and group
  position updates remain blocked on reader/sender and group-server state
  evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-group-flags\
  --filter "FullyQualifiedName~GroupFlagHandlerTests"` passed (`4/4`).

ZoneCompletion default-load follow-up:

- No new native labels were added. `ZoneCompletionRewardResolver` already uses
  `GameTableManager.Instance.ZoneCompletion` for the conservative
  exploration-only title reward path, and the native client data includes
  `DB\ZoneCompletion.tbl`. `GameTableManager.ZoneCompletion` now carries
  `[GameData]`, so default `GameTableManager.Initialise()` loads the table
  instead of leaving the resolver inert outside tests.
- Focused contract coverage now includes `ZoneCompletion.tbl` beside other
  runtime-required tables, and existing resolver tests still pass.
- Non-title rewards, faction/path/category-specific completion rows, and
  quest/challenge/datacube/journal objective-total sources remain blocked until
  those row semantics are decoded.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~GameTableManagerGameDataContractTests|FullyQualifiedName~ZoneCompletionRewardResolverTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed (`11/11`).

Transport, group, and loot packet-boundary follow-up:

- No runtime transport, group, or loot behavior was widened. Focused packet
  tests now cover the mapped `ClientVehicleEmbark` three-`uint32` payload,
  `ClientGroupLootRulesChange` / `ServerGroupLootRulesChange` four-rule
  layouts, `ClientLootRollAction`, `ClientLootAssignMaster`, and the current
  `ServerLootRoll`, `ServerLootWinner`, and `ServerLootRemove` layouts.
- The tests intentionally keep unresolved fields such as vehicle embark context
  values and `ServerGroupLootRulesChange.UnknownDWord` conservative. Vehicle
  boarding, seat/passenger modes, harvesting distribution, exact roll/master
  eligibility, bind-on-pickup confirmation, and unresolved loot auxiliary
  packets remain blocked on reader/sender and runtime evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~TransportPacketShapeTests|FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~LootPacketShapeTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed (`15/15`).

SimpleEntity archive/datacube activation coverage follow-up:

- No runtime behavior changed. Focused tests now cover both `OnActivate` and
  `OnActivateCast` for `ArchiveArticleIdInteractUnlock` with
  `grantRewards: false`, plus SimpleEntity datacube and journal activation
  calls through `IDatacubeManager`.
- This pins the existing activation boundary only. Full journal/datacube
  progression semantics, archive-link parent/child authorization, path-mission
  rule parity, and broader content hookups remain blocked until their row and
  trigger semantics are mapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~SimpleEntityArchiveUnlockTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed (`6/6`).

Guild recruitment and realm-first packet-boundary follow-up:

- No guild recruitment or achievement behavior was widened. Focused tests now
  pin `ClientRecruitmentGuildGetDetailedGuildInfo` as a 14-bit realm plus
  64-bit guild identity request, and `ServerRealmFirstAchievement` as a 15-bit
  achievement id, guild/player flag, and wide name payload.
- Recruitment output readers/subscriptions and persistent recruitment state
  remain blocked. Steam achievement payload grammar, achievement-id
  correlation, exact realm-first UI semantics, and remaining achievement
  trigger parity also remain blocked.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~GuildPacketShapeTests|FullyQualifiedName~RealmFirstAchievementPacketTests|FullyQualifiedName~ClientSteamAchievementsTests"
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` passed (`4/4`).

Marketplace commodity submit preflight follow-up:

- Added WildStar64 label `Marketplace_ValidateCommodityOrderBeforeSubmit`
  (`1406a04f0`) for the helper called immediately before
  `0x06DA ClientCommoditySellOrderSubmit`. The decompile shows the client
  accepts only new orders (`orderId == 0`), requires non-zero quantity and
  price, and caps quantity by `GameFormula` id `0x439` field `Dataint02`,
  falling back to `200` when the formula row is unavailable.
- `ClientCommoditySellOrderSubmitHandler` now applies that mapped preflight
  boundary server-side through `MarketplaceRequestHelper.ValidateCommodityOrder`.
  Item existence, non-zero quantity/price, and the `GameFormula` quantity cap
  are checked before the transient commodity order book is mutated.
- This does not implement durable commodity matching, partial fills, price
  precision, offline settlement, or mail settlement; those remain blocked on
  marketplace response readers and runtime settlement evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests"
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\marketplace-tests\`
  passed (`2/2`).

Marketplace commodity cancel success follow-up:

- No new native labels were added. This pass stays inside the already modeled
  commodity-cancel boundary: successful `ClientCommodityOrderCancel` now emits
  `ServerCommodityAuctionRemoved` with `AuctionEventType.Cancel` after the
  transient order is removed and its buy-order credits or sell-order item stack
  are refunded to the owner.
- Focused coverage now posts a transient commodity buy order, verifies the
  buyer credit debit, cancels it, verifies the credit refund, and checks the
  removed-order event carries the cancelled order id, item id, quantity,
  price fields, buy/sell flag, and cancel event type.
- Cancel failure/result precision, durable marketplace storage, commodity
  matching/partial fills, offline settlement, and marketplace mail settlement
  remain blocked on response-reader and runtime settlement evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-marketplace\
  --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests"`
  passed (`3/3`).

ICComm transient offline cleanup follow-up:

- No new native labels were added. This is a source-level coverage pass for the
  existing transient `ICCommManager.Update` cleanup: players whose `InWorld`
  flag is false are removed from joined ICComm channels, and an empty transient
  channel is removed from the channel indexes.
- Focused coverage now verifies that an offline member can no longer send to
  the old channel and that a lone offline member causes the emptied channel to
  be discarded before the same channel name is joined again.
- Exact leave/logout client signaling, persistent channel membership,
  entitlement checks, throttling, and directed-vs-ordered packet precision
  remain blocked on stronger native/client behavior evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-iccomm\
  --filter "FullyQualifiedName~ICCommManagerTests"` passed (`2/2`).

ICComm transient delivery follow-up:

- No runtime behavior changed. Focused coverage now pins the current transient
  `ICCommManager.SendMessage` delivery boundary: successful sends enqueue a
  `ServerICCommMessageResult` plus `ServerICCommOrderedMessage` to the sender
  and `ServerICCommDirectedMessage` to eligible online recipients.
- Named-recipient sends are case-insensitive and deliver only to the matching
  online channel member. Missing named recipients return `NotInChannel` without
  sender echo/result packets.
- Retail delivery precision, exact leave/logout client signaling, entitlement
  checks, persistent channels, throttling, and auxiliary chat/ICComm outputs
  remain blocked on stronger client behavior evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-iccomm-delivery\
  --filter "FullyQualifiedName~ICCommManagerTests"` passed (`5/5`).

Mail delete/return manager follow-up:

- No runtime behavior changed. Focused manager coverage now pins successful
  `MailDelete` as an `IMailItem.EnqueueDelete(true)` call plus
  `ServerMailUnavailable` and `ServerMailResult(Delete, Ok)`, and missing-mail
  delete as `ServerMailResult(Delete, MailDoesNotExist)` with no unavailable
  notification.
- Return-mail coverage now pins the existing successful path as removing the
  mail from available mail, queueing the same item into outgoing mail, calling
  `IMailItem.ReturnMail`, sending `ServerMailUnavailable`, and reporting
  `ServerMailResult(Send, Ok)`. Not-returnable mail stays available, does not
  queue outgoing mail, and reports `MailCannotReturn`.
- Marketplace/offline settlement, save/reload delete-state parity,
  multi-client mailbox views, exact expiration timing/results, and broader
  attachment/cash atomicity remain blocked on stronger runtime/client evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-mail-delete-return\
  --filter "FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests"`
  passed (`11/11`).

Fortune account-scoped session follow-up:

- No runtime behavior changed. Focused coverage now pins that the conservative
  `FortuneSessionManager` stores card state by account id: a flipped card for
  one account does not affect another account's status response.
- Starting a new fortune session for the same account replaces the prior
  in-memory state and returns unflipped update cards.
- Reward payout, eligibility/cost, real reward selection, storefront/game
  synchronisation, and durable retail resume behavior remain blocked on
  stronger fortune UI and account-inventory evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-fortune-session\
  --filter "FullyQualifiedName~FortuneSessionManagerTests"` passed (`7/7`).

Support report capture follow-up:

- No runtime behavior changed. Focused coverage now pins the current support
  capture boundary for incident reports, bug reports, and suggestions.
  Incident reports validate `ReportPlayerReason` and `ReportPlayerSource`
  before writing the `incident` submission payload; bug reports validate
  `BugCategory` before writing the `bug` payload; suggestions always write the
  `suggestion` payload.
- These paths intentionally do not enqueue client result packets today. Only
  support-ticket success/failure has a deterministic `ServerSupportTicketResult`
  boundary.
- Moderation workflow, durable database-backed case lifecycle, user-facing
  support-case result/readback semantics, stuck-result client signaling, and
  report/survey administrative tooling remain blocked on stronger client/server
  evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-support-capture\
  --filter "FullyQualifiedName~SupportTicketHandlerTests|FullyQualifiedName~CustomerSurveyProtocolTests"`
  passed (`15/15`).

Support stuck branch coverage follow-up:

- No runtime behavior changed. Focused coverage now pins the conservative
  `ClientStuckHandler` branch behavior for the currently implemented
  `/stuck` paths.
- Invalid `UnstickType` values do not mutate the player. `FreeSuicide` only
  calls `ModifyHealth` for living players, uses `DamageType.Physical`, targets
  the player as the source, and clamps zero current health to one damage.
  `RecallTransmat` respects `IPlayer.CanTeleport()` before attempting a
  teleport.
- Client-facing stuck result signaling, exact recall-house/transmat destination
  parity, spell-cast side effects, and cooldown/result UI behavior remain
  blocked on stronger client/server evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-support-stuck\
  --filter "FullyQualifiedName~SupportTicketHandlerTests|FullyQualifiedName~SupportStuckHandlerTests|FullyQualifiedName~CustomerSurveyProtocolTests"`
  passed (`20/20`).

Crafting LootId output coverage follow-up:

- No runtime behavior changed. The existing fixed-recipe crafting LootId output
  path now has focused coverage: a schematic with `LootId` and no direct item
  output calls `IGlobalLootManager.TryGenerateLoot`, validates generated loot
  delivery, debits satchel material, gives generated loot to the crafter with
  granted-item notification enabled, and reports the first static generated
  item id in `ServerCraftingFinish`.
- The test helper had a compile-only generic constraint drift; `CreateGameTable<T>`
  now matches `GameTable<T>`'s `where T : class, new()` constraint.
- Discovery rolls, station constraints, broader random-output parity, material
  source precision, rune item data, and sigil result meanings remain blocked on
  stronger crafting reader/table evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-crafting-lootid\
  --filter "FullyQualifiedName~CraftingLootIdCraftHandlerTests|FullyQualifiedName~CraftingSimpleCraftHandlerTests"`
  passed (`4/4`).

Option handler coverage follow-up:

- No runtime behavior changed. Focused coverage now pins the mapped option
  handlers: `ClientCombatOptions` updates casting, disable-other-player-log,
  and combat-log disable preferences; `ClientOptions` updates casting and
  shared-challenge preferences; `ClientCombatLogDisableOthers` and
  `ClientCombatLogDisables` update their dedicated combat-log preferences.
- Validation coverage now rejects unknown casting bits, unknown combat-log bits,
  invalid shared-challenge values, and invalid option types before mutating the
  player proxy.
- Additional option types, account-vs-character option split, exact
  client-facing initialisation/readback, and nearby item/options server opcodes
  remain blocked on result/readback packet evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-options\
  --filter "FullyQualifiedName~OptionHandlerTests|FullyQualifiedName~OptionPersistenceTests"`
  passed (`13/13`).

Realm select compatibility follow-up:

- No runtime behavior changed. Focused coverage now pins the conservative
  `ClientSelectRealmHandler` behavior at the current compatibility boundary:
  selecting the already-connected realm is ignored, and selecting an offline
  target realm sends `ServerRealmTransferResult` with
  `RealmTransferFailed_ServerDown`.
- These tests keep the handler non-mutating for unsupported online transfer
  flows. Real realm-transfer destinations/results, session-key handoff, PTR
  copy state, new realm notices, optional realm messages, and character admin
  result packets remain blocked on stronger client reader and server handoff
  evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-pregame-realm\
  --filter "FullyQualifiedName~RealmTransferProtocolTests"` passed (`4/4`).

Leaderboard packet-row coverage follow-up:

- No runtime behavior changed. Focused packet-shape coverage now pins non-empty
  `ServerLeaderboardPve` and `ServerLeaderboardPvp` response rows, including
  row rank/last-rank values, PVE scope fields, PVP rating, row names, and nested
  team-member name/class entries.
- The default `EmptyLeaderboardProvider` remains a deterministic compatibility
  provider. Real ranking rows, personal placement, aggregation, cache/season
  behavior, and category/filter semantics remain blocked until the client UI
  readers and a score source-of-truth are mapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-leaderboard\
  --filter "FullyQualifiedName~Leaderboard"` passed (`6/6`).

Challenge packet-boundary coverage follow-up:

- No runtime behavior changed. Focused coverage now pins the mapped challenge
  protocol boundary: `ClientChallengeChoice` reads a 14-bit challenge id, 5-bit
  choice, and trailing uint32; `ServerChallengeResult`, `ServerChallengeShared`,
  `ServerChallengeShareTimeout`, and non-empty `ServerChallengeUpdate` active
  rows preserve the documented field order and bit widths.
- `ClientChallengeChoiceHandler` remains intentionally non-mutating and returns
  `ChallengeResult.GenericFail` for compatibility. Active lifecycle, shared
  accept/decline state, update cadence, reward tiers, and quest/objective
  integration remain blocked until the retail accept/update/result sequence is
  mapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-challenges\
  --filter "FullyQualifiedName~Challenge"` passed (`9/9`).

Guild recruitment packet-boundary coverage follow-up:

- No runtime behavior changed. Focused coverage now pins the currently modeled
  guild recruitment wire boundary: detailed guild info request identity,
  recruitment details, guild list rows with recruiters, guild update rows
  without recruiter lists, 12-role demand payloads, and recruiter name/online
  flag ordering. Existing war-party boss-token request/response coverage still
  pins the empty/result-compatible token payload shape.
- Recruitment subscription/update timing, bank transactions, perks, holomarks,
  standards, real war-party boss-token inventory/results, and warplot plug state
  remain blocked until the recruitment/warparty readers and persistent state
  transitions are mapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-guild-protocol\
  --filter "FullyQualifiedName~GuildPacketShapeTests|FullyQualifiedName~WarPartyBossTokenProtocolTests"`
  passed (`7/7`).

Transport vehicle passenger packet-boundary follow-up:

- No runtime behavior changed. Focused transport coverage now pins the vehicle
  passenger output family alongside the existing rapid-transport,
  flight-path, and vehicle-embark packet shapes: `ServerVehiclePassengerSelf`,
  `ServerVehiclePassengerAdd`, `ServerVehiclePassengerRemove`, and shared
  `VehiclePassenger` rows preserve vehicle/passenger ids plus the 2-bit seat
  type and 3-bit seat position layout.
- Service-token bypass, taxi route state, taxi embark/completion, charge and
  teleport rules, passenger seat-mode effects, deployable vehicle semantics,
  and nearby unresolved `Server0x077E` remain blocked on reader/table evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-transport\
  --filter "FullyQualifiedName~TransportPacketShapeTests"` passed (`9/9`).

PvP and duel packet-boundary follow-up:

- No runtime behavior changed. Focused PVP coverage now pins the currently
  modeled duel/PVP wire boundary: duel challenge/countdown/start unit pairs,
  5-bit duel failure reasons, 3-bit duel finish reasons, cooldown update
  milliseconds, 3-bit unit PVP state flags, zero-byte warning/left-area/clear
  packets, and the client PVP/ignore-duel one-bit toggles.
- Observer broadcasts, duel leash/warning lifecycle, persistent PVP cooldown,
  forced-map PVP, reward/stat settlement, and exact cancel/result semantics
  remain blocked until the duel warning/result readers and runtime evidence are
  mapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-pvp\
  --filter "FullyQualifiedName~Pvp|FullyQualifiedName~Duel"` passed (`12/12`).

Account unlock and costume packet-boundary follow-up:

- No runtime behavior changed for unlock delivery. Focused coverage now pins
  `ClientItemGenericUnlock` item-location parsing, single generic unlock
  grants, account/character generic unlock list and refresh arrays, 3-bit
  generic unlock results, costume item unlock/forget request payloads, and
  single/multiple costume unlock result payloads.
- Generic unlock persistence, account-vs-character delta timing, item
  eligibility, supply satchel precision, pet flair lifecycle, and broader
  costume forget/unlock parity remain blocked on item/unlock reader and state
  transition evidence.
- While verifying this slice, two shared compile blockers from adjacent housing
  work were cleared: the untracked `ResidenceNeighborInviteInfo` source now
  builds through `NexusForever.Game.Abstract`, and `HousingPacketShapeTests`
  now uses the existing `GamePacketReader.ReadInt()` API for counted neighbor
  rows.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-unlocks-fresh4\
  --filter "FullyQualifiedName~AccountUnlockPacketShapeTests|FullyQualifiedName~AccountItemHandlerTests"`
  passed (`12/12`).

Action-set and AMP packet-boundary follow-up:

- No runtime behavior changed. Focused coverage now pins client
  `ClientRequestActionSetChanges`, `ClientNonSpellActionSetChanges`, and
  `ClientCommitAmpSpec` request parsing plus `ServerActionSet`,
  `ServerAmpRespecResult`, `ServerSpecChanged`, and
  `ServerActionSetClearCache` payload order and bit widths.
- `UpdateSpellInProgress`, the async spell update transaction, authoritative
  attribute allocation/refund, bonus ability/AMP unlock persistence, and
  action-bar lock state remain blocked on the unresolved
  `Server0x00B0/016B/016D/016E/019C/01A4` cluster and native transaction
  evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-actionset\
  --filter "FullyQualifiedName~ActionSetPacketShapeTests|FullyQualifiedName~ActionSetAmpTests"`
  passed (`7/7`).

Datacube and Galactic Archive packet-boundary follow-up:

- No runtime behavior changed. Focused coverage now pins datacube/journal
  update packets (`ServerDatacubeUpdateList`, `ServerDatacubeUpdate`,
  `ServerDatacubeVolumeUpdate`) plus galactic archive unlock/view client
  requests, archive update flags, and zero-byte archive refresh.
- Broader content hookups, archive-link parent/child authorization, full
  journal/datacube progression semantics, path-mission rule parity, and wider
  Codex UX behavior remain blocked on content-flow and reader evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-archive\
  --filter "FullyQualifiedName~ArchivePacketShapeTests|FullyQualifiedName~GalacticArchiveUnlockRuleTests|FullyQualifiedName~SimpleEntityArchiveUnlockTests"`
  passed (`14/14`).

Zone-map packet-boundary follow-up:

- No runtime behavior changed. Focused coverage now pins `ServerZoneMap`
  output for map id, byte count, and raw bit-buffer ordering, plus
  `ServerMapUpdateHexGroup` output for the 14-bit hex-group id, 21-bit tooltip
  localized text id, color, and visible flag.
- Full zone-completion semantics, non-title rewards, faction/path-specific
  rows, and quest/challenge/datacube/journal objective totals remain blocked on
  completion row category/faction semantics and UI reader evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-zonemap\
  --filter "FullyQualifiedName~ZoneMapPacketShapeTests|FullyQualifiedName~ZoneCompletionRewardResolverTests"`
  passed (`5/5`).

Achievement packet-boundary follow-up:

- No runtime behavior changed. Focused coverage now pins `ServerAchievementInit`
  counted rows, `ServerAchievementUpdate` deleted flag plus rows, and the
  existing `ServerRealmFirstAchievement` achievement id/guild/name layout.
- Full trigger coverage, Steam achievement payload grammar and achievement-id
  correlation, exact realm-first broadcast semantics, remaining updater parity,
  and achievement UI edge cases remain blocked on reader/runtime evidence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-achievements\
  --filter "FullyQualifiedName~Achievement"` passed (`15/15`).

Housing early-cluster opcode naming (`0x00CB..0x00D1`, `0x010D`, `0x0110`, 2026-05-23):

- Replaced `Server0x00CB`..`Server0x00D1` and `Server0x010D`/`Server0x0110` with reader-backed
  names in `GameMessageOpcode.cs` and `ServerHousingAuxPackets.cs`:
  `ServerHousingResidenceEmpty`, `ServerHousingResidenceUInt15`, `ServerHousingResidenceUInt15Alt`,
  `ServerHousingResidenceWideString`, `ServerHousingResidenceEmptyFollowUp`,
  `ServerHousingBasicsEmpty`, and `ServerHousingBasicsFollowup`.
- Consumer intent and emit sites remain blocked; `PacketPlaceholderNamingTests` housing cluster
  coverage was updated to the new type names.
- Corrected `Server0x077E` enum comment: it is a 16-byte fixed payload between recruitment and
  pet despawn, not `ServerFlightPathUpdate_ReadPayload` (`0x0188`).

Spell broadcast opcode naming (`0x0814..0x0819`, 2026-05-23):

- `GameMessageOpcode` now names the mapped spell-threshold and spell-wrapper follow-up cluster:
  `ServerSpellThresholdClear` (`0x0814`), `ServerSpellThresholdSpell4` (`0x0815`),
  `ServerSpellThresholdStart` (`0x0816`), `ServerSpellThresholdUpdate` (`0x0817`),
  `ServerSpellWrapperTierEntry` (`0x0818`), and newly added `ServerSpellWrapperNodeRemove` (`0x0819`).
- Evidence: WildStar64 registration/dispatch in `FUN_14006c290`, consumer paths documented in
  `Decomp/Analysis/SPELL_BROADCAST_ROADMAP.md`, and focused wire-shape tests in
  `SpellBroadcastPacketShapeTests`.
- Runtime emitters for threshold/wrapper packets remain blocked; this pass is enum/model naming only.

Opcode model cleanup and coverage refresh follow-up:

- `ServerUnresolvedOutputPackets.cs` was reconciled after a partial checkout
  drift: the named `ServerCREDDExchangeInfoResults`, provisional group/raid
  raw payloads, and mapped account/store packet classes again own their
  corresponding `GameMessageOpcode` enum names. The old `Server0x026A`,
  `Server0x0414/042A/0431/0436/0438/0441/045A/0461/0468/0718`, and mapped
  account/store `Server0x0969/096E/0971/0976/0977/0978/097B/097D/097E/0980/
  0989/098A/098E/0990/0991` enum-reference forms no longer appear in source.
- Removed the stale duplicate `ClientRealmSelect = 0x07DF` enum alias. The
  authoritative packet model and handler remain `ClientSelectRealm` for opcode
  `0x07DF`.
- A production opcode audit now reports `1041` enum members, `1041`
  production `[Message]` models, no dangling message references, no duplicate
  message mappings, no enum members without models, and no duplicate numeric
  opcode values. The refreshed coverage snapshot reports client `346/346`,
  server `692/692`, and core `3/3`, with no missing models or handlers.
- Verification:
  `dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-world\`
  passed, and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-current\`
  passed (`508/508`).

Spell runtime, entities, content, progression workstream (`F-016`..`F-036`, 2026-05-22):

- F-017 now has durable combat-log helper labels for damage/heal/shield/absorb events,
  and runtime widening is limited to semantics backed by table/export evidence or focused
  packet-shape tests. Status below uses the evidence ladder from
  `Decomp/Analysis/CONTINUATION_GUIDE.md`: **Partial** = conservative runtime with focused tests;
  **Mapped-only** = packet/table/script surface pinned without safe retail mutation.

| Row | Status | Key surface | Tests / smoke | Blockers |
| --- | --- | --- | --- | --- |
| F-016 Procs | Partial | `ProcDispatchEvidenceBoundary`, holder-side dispatch for events `1/6/10/12/16/17/18/19/20`, routes `1/2/9` holder and `4/12` counterpart, `ProcRuntimeEvidenceCollector` + `!spell procreport`; witness `Spell4=4046` pinned in `ProcFixtureWitnessTests` | `ProcDispatchEvidenceBoundaryTests`, `ProcTargetDataCandidateTests`, `ProcTriggerEventCandidateTests`, `ProcRuntimeEvidenceCollectorTests`, `ProcFixtureWitnessTests` | Unsupported trigger events; `targetData` tails `14/18/20/33/34/36`; exact chance/cooldown ordering; recursion beyond same-chain block; `ServerSpellUInt32TripletList`, `ServerSpellUInt32TripletListVariant`, and `ServerSpellFourUInt32` row/field semantics |
| F-017 Damage/heal/shields/vitals | Partial | Family-first handlers in `SpellEffectHandler` / `SpellEffectInterpreter`; diagnostics for damage, heal, shields, absorption, vital/sap/clamp families per `Spell Effect Evidence Matrix.md`; damage formula path now applies AP/SP missing-formula fallback and damage-type-specific armor offsets; absorption uses `DataBits04` school masks; observed `ClampVital` rows are health clamps; damage/healing-absorption combat logs carry mapped context; health damage derives killed/overkill result state and bounded tick damage logs set `bPeriodic`; witness `Spell4=82315` pinned in `DamageShieldFixtureWitnessTests` | `DamageCalculatorRetailParityTests`, `AbsorptionSemanticsTests`, `ClampVitalSemanticsTests`, `CombatLogPacketShapeTests`, `UnitEntityDamageResultTests`, `DamageShieldFixtureWitnessTests`, plus spell proc/boundary tests | Remaining retail formula/rounding, weapon/DPS, and item-budget semantics; distance/distribution splitting; HoT tick-only/dynamic targeting; `ServerSpellEffectDamage` emit policy; shield/absorb packet parity; exact SapVital modes; unsupported alias vitals; non-health ClampVital modes; fixture captures before widening |
| F-018 CC/stacks/movement | Partial | `CCStateSet`/`CCStateBreak`, timed removal, cast/movement coupling; packet models; witness `Spell4=57355` maps `DataBits00=20` to `CCState.Tether` in `CCStateSetFixtureWitnessTests` | `CrowdControlPacketShapeTests`, `CCStateSetFixtureWitnessTests` | `Spell4StackGroup` arbitration; DR/stun breakout; tether/additional-data; forced-move/facing physics parity |
| F-019 Summons/traps/vehicles | Partial | `SummonCreature`, `SummonTrap`, `SummonVehicle`, `NpcExecutionDelay` conservative create/hold paths; `SummonTrapEvidenceBoundary` centralizes create gating for witness `Spell4=34094` | `SummonTrapEvidenceBoundaryTests`; summon families still covered indirectly elsewhere | Ownership/AI controller; trap trigger spells; formation/service payloads; turret/deployable seat modes |
| F-020 RavelSignal | Partial | `RavelSignalReceiverEvidenceBoundary` maps mode `1` to `EntityScriptOnSignal` (witness `Spell4=76797`, signal `27096`); other modes stay diagnostics-only; `HandleEffectRavelSignalCore` gates `SendSignal` through the boundary | `RavelSignalReceiverEvidenceBoundaryTests` | Additional mode receivers, payload-driven script state, `SpellRouteEvent_*` producer timing |
| F-021 LAS/action-set/AMP | Partial | `ClientRequestActionSetChangesHandler`, LAS tier/AMP persistence (`ActionSetAmp.Save`), `LimitedActionSetResult.UpdateSpellInProgress` enum | `ActionSetAmpTests`, new `ActionSetPacketShapeTests` | `Server0x00B0`, `016B/016D/016E/019C/01A4` async spell-update cluster; authoritative attribute refund; bonus AMP unlock persistence |
| F-022 Quests/path/public events | Partial | `QuestManager`, `PublicEvent` scripts/objectives, path manager surfaces | `QuestTests`, `QuestObjectiveTests`, `PublicEventFlowTests`, `PublicEventObjectiveTests`, `PathManagerTests` | Path mission edge types; PE votes/scoreboards; `Server0x0139/06F7`; quest-share precision |
| F-023 NPE / Rider's Reef | Mapped-only | Tutorial scripts under `Script.Main/Tutorial`; recent reef/departure fixes per matrix | Manual client smoke required (`I:\WildStar`); login-world smoke reported working | Quest acceptance/kill loops, rewards, respawn, CSI, hoverboard/projector, final terminal - no automated pass in CI |
| F-024 World 3404 | Partial | `Script.Instance/Expedition/EvilFromTheEther`, staged map/import | `EvilFromTheEtherEventScriptTests`, `EvilFromTheEtherTriggerScriptTests` | Manual expedition smoke; PE `781`; doors/interactables/teleports/phases; unsupported spell blockers in live play |
| F-025 Entity create/update/phasing/CSI | Partial | `ServerEntityCreate` world-placement path, busy/interaction gates | `EntityCreatePacketTests` | Remaining create/update substructures `025F..0264`; deferred action queues; CSI/current-target; phase visibility; `0889/08CC/08F4/0939/093D/093E` |
| F-026 Items/unlocks/costumes/pets | Partial | Inventory, generic unlock handler, costume/pet/title managers (parts) | `AccountItemHandlerTests` (generic unlock lifecycle) | Item swap/error aux `00B7/0183/019A/037F/0567`; satchel precision; costume forget; unlock list deltas |
| F-029 Client DB / DataMapping | Partial | `GameTableManager` default init includes `ZoneCompletion.tbl` and archive tables; DataMapping tooling | `GameTableManagerGameDataContractTests` | Case-by-case staging promotion; EF placeholder renames; migration proof per table |
| F-034 Datacubes/archive | Partial | `DatacubeManager`, `GalacticArchiveManager`, login init packets; `ArchiveArticleIdInteractUnlock` on `SimpleEntity` | `SimpleEntityArchiveUnlockTests`, `GalacticArchiveUnlockRuleTests`, `PlayerTradeskillArchiveTests` | Archive-link parent/child auth; full journal/datacube progression; path-mission rule parity |
| F-035 Achievements / realm-firsts | Partial | Character/guild/global managers, init/update packets, many updaters | `AchievementProgressTests`, `GuildAchievementManagerTests`, `RealmFirstAchievementPacketTests`, `ClientSteamAchievementsTests` (diagnostic-only ingest) | Full trigger coverage; Steam payload grammar ? achievement-id map; exact realm-first broadcast semantics |
| F-036 Zone maps / completion | Partial | Hex discovery, `ServerZoneMap`, `ZoneCompletion.tbl` loaded; exploration-only title rewards via `ZoneCompletionRewardResolver` | `ZoneCompletionRewardResolverTests` | Non-title rewards; faction/path-specific rows; quest/challenge/datacube/journal totals integration |

- Verification (workstream-filtered): `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\workstream-rows-tests\`
  passed `185/185` for proc/spell/CC/quest/public-event/entity/item/zone/achievement/archive/game-table filters.
- Spell broadcast follow-up (`0x07F5..0x0819`) remains mapped structurally per `Decomp/Analysis/SPELL_BROADCAST_ROADMAP.md`;
  threshold packets `0814/0816/0817` and conflicted `0818` are not enabled on live send paths.

Protocol semantics pass (2026-05-22, PROTOCOL SEMANTICS workstream):

- Store terminal cluster: renamed `0x098C` to `ServerStorePurchaseOfferResult`,
  `0x098D` to `ServerStorePurchaseOfferResultVariant`, `0x098F` to
  `ServerStoreCurrencyPackageRow`, and `0x0986` to `ServerAccountUInt64Payload`.
  Client consumer semantics for `098C/098D` remain mapped to
  `StorePurchaseOfferResult`; opcode-variant distinction and success emit sites
  stay blocked. `0986` and `098F` stay diagnostic-only.
- Housing privacy/instance cluster: `FUN_14008de20` maps `0x00CA` to
  `ServerHousingResidenceKeyedUpdate` (64-bit key + one uint32 field). The
  `FUN_14006c290` registration pass maps `0x00CB`, `0x00D1`, and `0x010D` to
  the shared empty reader, `0x00CC` and `0x00CD` to a 15-bit scalar reader,
  `0x00CE` to the shared wide-string reader, and `0x0110` to a housing-basics
  follow-up of uint32, 18-bit field, uint32, and 8-bit field. Consumer intent
  and safe server emit sites remain blocked.
- Spell auxiliary cluster: `FUN_140095da0` maps `0x080F` and `0x0810` to counted
  uint32-triplet lists (`ServerSpellUInt32TripletList` /
  `ServerSpellUInt32TripletListVariant`). `FUN_14007fef0` maps `0x0812` to
  `ServerSpellFourUInt32`. Row/field gameplay semantics and runtime emit paths
  remain blocked.
- F-001 STS token crypto: mapped handshake documented (LoginTokenStart ->
  server key material -> TokenKeyData); implementation remains blocked pending
  RSA/signature verification evidence and STS captures. See seventh follow-up
  under STS auth above.
- F-002 client diagnostics: all `17` `Client0xNNNN` handlers remain
  non-mutating; native writer functions for `003D`, `00C8`, `00ED`, etc. were
  not recovered in this pass.
- **Resolved (2026-05-23):** all `Server0xNNNN` enum placeholders were renamed to
  cluster/shape names; see the `Server0x elimination` follow-up at the end of
  this file. Runtime emit semantics for those packets remain blocked where noted.

Housing neighbor list packet follow-up:

- `FindImmediateInstructions` for `0x0507` found the world opcode registration
  in `FUN_14006c290` at `140077761`; the registration uses reader
  `FUN_14009ecc0` and a `0x10`-byte packet object.
- `ServerHousingNeighbors_ReadPayload` (`WildStar64.exe:14009ecc0`) reads a
  32-bit row count, allocates `count * 0x28` bytes, then parses each row through
  `ServerHousingNeighbors_Row_ReadPayload` (`WildStar64.exe:14009cf10`).
- `ServerHousingNeighbors_Row_ReadPayload` reads two consecutive uint64 fields,
  then `NetworkBitReader_ReadIdentity`, then one uint32 field. The follow-up
  cache/UI pass maps row `+0` as the neighbor character id retained on the
  cached neighbor entry, row `+8` as a reserved value that is not copied into the
  native neighbor cache or exposed through `GetNeighborList`, the identity as
  the target residence key used by evict/permission/invite-result flows, and the
  uint32 as the raw permission value. NexusForever now writes the character id,
  zero reserved slot, target residence identity, and permission; the previous
  wide-string row shape and duplicate residence-id slot were wrong for the
  native reader and consumer path.
- Evidence logs:
  `Decomp/Analysis/logs/runs/housing_neighbors_0507_immediate/WildStar64.NexusForeverClient64_WildStar64.FindImmediateInstructions.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_0507_reader/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_read_payload/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_row_read/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_cache_constructor_probe/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_table_builder_probe/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_update_consumer_probe/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_selection_probe/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/housing_neighbors_evict_lua_probe/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  and
  `Decomp/Analysis/logs/runs/housing_neighbors_permission_lua_probe/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`.

Housing community donate remap (`0x04FE`):

- `FindImmediateInstructions` for `0x04FE` found dispatch registration in
  `FUN_14006c290` at `14007762f` with reader `FUN_14009e930` (`0x18`-byte packet
  object).
- `FUN_14009e930` reads a uint32 count, then two parallel uint32 arrays of that
  length. NexusForever models this as `ServerHousingCommunityDonateUpdate` and
  emits source/target decor id pairs (low 32 bits) after
  `ClientHousingCommunityDonate` copies crate decor into the community residence.
- Evidence logs:
  `Decomp/Analysis/logs/runs/housing_04fe_dispatch/WildStar64.NexusForeverClient64_WildStar64.FindImmediateInstructions.ghidra.log`
  and
  `Decomp/Analysis/logs/runs/housing_04fe_reader/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`.

Housing community donate client sender follow-up (`0x04F5`):

- `0x04F5` registers in `FUN_14006c290` with write/read helper pair
  `LAB_14009d9d0` and `FUN_14009da00`. `FUN_14009da00` serialises a uint32
  count followed by that many full `DecorInfo` rows, matching
  `ClientHousingCommunityDonate`.
- The sender at `Housing_SendClientCommunityDonate`
  (`WildStar64.exe:1404b9ca0`) builds a single-row donation packet from the
  selected decor id, resolves the row's `HousingDecorInfo`, and refuses to send
  when `HousingDecorInfo.Flags & 0x8` is set. `HousingDecorInfoEntry` confirms
  `Flags` at table offset `0x10`.
- NexusForever now mirrors the verified no-donate guard server-side: donation
  rows are validated before any residence mutation, and a no-donate row returns
  `HousingResult.Decor_CannotDonate` without copying or deleting decor.
- Evidence logs:
  `Decomp/Analysis/logs/runs/inspect_housing_donate_reader/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_housing_donate_writer_stub/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  and
  `Decomp/Analysis/logs/runs/inspect_housing_donate_sender/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`.
- Still blocked: exact community resource/contribution cost semantics, item
  refund behavior, and ownership/inventory transfer rules are not proven by this
  sender path and remain mapped-only blockers under F-004.

Housing decor update payload follow-up (`0x050B`):

- `FindImmediateInstructions` for `0x050B` found the dispatch registration at
  `FUN_14006c290` and multiple decor senders near `1404b8280..1404b97b0`.
  The registered writer `ClientHousingDecorUpdate_WritePayload`
  (`WildStar64.exe:14009d760`) serialises a 3-bit operation, a uint32 row count,
  that many full `DecorInfo` rows, and then one trailing bit for each row.
- NexusForever previously consumed only one trailing bit regardless of row
  count. `ClientHousingDecorUpdate` now preserves the native per-row trailing
  flags diagnostically as `TrailingFlags`, with packet coverage for multi-row
  payloads.
- `Housing_BuildDecorUpdateRow` (`WildStar64.exe:1404b89a0`) confirms that the
  row data is filled from cached residence decor state and selected target
  residence identity before the `0x050B` senders submit create/move/delete
  operations. The trailing flag semantics remain unresolved, so runtime
  behavior does not branch on them yet.
- Evidence logs:
  `Decomp/Analysis/logs/runs/inspect_housing_decor_update_immediate/WildStar64.FindImmediateInstructions.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_housing_decor_update_reader/WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_housing_decor_update_sender_move/WildStar64.InspectCodeAddress.ghidra.log`,
  and
  `Decomp/Analysis/logs/runs/inspect_housing_decor_update_row_builder/WildStar64.InspectCodeAddress.ghidra.log`.

Housing neighbor opcode-cluster follow-up (`0x0514`, `0x0516`, `0x0519`,
`0x051F`):

- `0x0514` dispatch registration in `FUN_14006c290` uses
  `ServerHousingNeighborInvitePrompt_ReadPayload` (`WildStar64.exe:140091ef0`),
  a `0x18`-byte packet object, target residence identity, and one wide string.
  NexusForever now emits this native prompt to the invited player instead of
  overloading generic `ServerHousingResult` for the pending invite UI.
- `0x0516` dispatch registration uses
  `ServerHousingNeighborInviteResult_ReadPayload`
  (`WildStar64.exe:14009edd0`),
  a `0x30`-byte packet object, the shared `ServerHousingNeighbors` row reader,
  and one trailing 7-bit field. NexusForever models the packet as
  `ServerHousingNeighborInviteResult` and writes the trailing field as
  `HousingResult`. `Housing_HandleNeighborInviteResult`
  (`WildStar64.exe:1404bb380`) special-cases
  `HousingNeighborInviteAccepted` for `HousingResult_Neighbor_RequestAccepted`
  and `HousingNeighborInviteDeclined` for the declined invite results before
  falling back to the generic `HousingResult` event.
- `0x0519` dispatch registration uses
  `ServerHousingNeighborUpdate_ReadPayload` (`WildStar64.exe:14009ed80`),
  the shared neighbor row reader, and one trailing 3-bit field. NexusForever
  models this as `ServerHousingNeighborUpdate` for add, permission-change,
  remove, and refresh-style neighbor row deltas. `Housing_HandleNeighborUpdate`
  (`WildStar64.exe:1404bb790`) branches on action values `0..3` to add,
  mutate, remove, or refresh cached neighbor entries.
- `0x051F` dispatch registration uses
  `ServerHousingCommunityPlotReservation_ReadPayload`
  (`WildStar64.exe:140086e70`), target residence identity, and one uint32. The
  HousingLib community plot helpers prove the uint32 is the reserved plot
  index: `ReserveCommunityPlot` (`WildStar64.exe:140737350`) sends guild
  operation `0x29` through the shared `ClientGuildOperation` opcode `0x04B1`,
  `RemoveCommunityPlotReservation` (`WildStar64.exe:140737400`) sends operation
  `0x2A`, and `GetReservedCommunityPlotIndex`
  (`WildStar64.exe:140737470`) returns the cached `nPlotIndex` when present
  with `0xffffffff` as the no-reservation sentinel. NexusForever now models
  `0x051F` as `ServerHousingCommunityPlotReservation` and emits it after
  successful community plot reservation or reservation-removal operations.
- Labels now appear in
  `Decomp/Analysis/exports/WildStar64.exe/functions.csv`,
  `selected_reasons_summary.csv`, and `selected_decompiled.c`.
- Verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\world-server-housing-neighbor-cluster\`
  passed, and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-housing-neighbor-cluster\
  --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"`
  passed `49/49`.

Housing neighborhood/community placement cluster follow-up (`0x0501`, `0x0506`,
`0x053A`, `0x053B`):

- `0x0501` dispatch registration in `FUN_14006c290` points at the shared
  `ServerHousingNeighborhoodEntry_ReadPayload` row reader
  (`WildStar64.exe:14009cbe0`) with a `0x30`-byte packet object. The row reads
  one uint64, two 14-bit fields, one uint64, one wide string, and three uint32
  fields.
- `0x0506` dispatch registration uses
  `ServerHousingNeighborhoodList_ReadPayload` (`WildStar64.exe:14009ebf0`), a
  `0x10`-byte packet object, one 14-bit realm field, a uint32 row count, and
  count `0x30`-byte `ServerHousingNeighborhoodEntry` rows. The client consumer
  `Housing_HandleNeighborhoodList` (`WildStar64.exe:1404ba4f0`) clears and
  repopulates the cached neighborhood list before dispatching
  `HousingNeighborhoodRecieved`.
- `0x053A` dispatch registration uses
  `ServerHousingCommunityPlacement_ReadPayload` (`WildStar64.exe:140092660`)
  and reads target residence identity, one uint64, and one uint32. The
  `HousingLib.RequestCommunityPlacement` path
  (`Lua_HousingLib_RequestCommunityPlacement`, `WildStar64.exe:140736950`)
  calls `Housing_SendClientCommunityPlacement` (`WildStar64.exe:1404b6b90`),
  which sends client opcode `0x052B` with the target community residence and
  requested property index. Correlating that request with the placed-residences
  list consumer (`HousingCommunityPlacedResidencesListRecieved`, rows exposing
  `nPropertyIndex`) maps server `0x053A` as the delta carrying target community
  residence, placed residence id, and property index. NexusForever now validates
  the target community identity and emits `ServerHousingCommunityPlacement`
  after a successful placement move.
- `0x053B` dispatch registration uses
  `ServerHousingCommunityPrivacyLevelUpdate_ReadPayload`
  (`WildStar64.exe:14009e8a0`) and reads target residence identity, two uint32
  fields, and one trailing 3-bit value. `Lua_HousingLib_SetCommunityPrivacyLevel`
  (`WildStar64.exe:1407376f0`) accepts Lua values `0` and `3`, sends client
  opcode `0x0538` with a one-bit private flag, and the adjacent
  `GetCommunityPrivacyLevel` code reads the type-7 community cache row flag
  bit `0x10`, returning Lua value `3` when private and `0` otherwise.
  NexusForever now validates the target community identity and emits
  `ServerHousingCommunityPrivacyLevelUpdate` with entry type `7`, private flag
  `0x10` when private, and the native Lua privacy value `0`/`3`.
- Labels now appear in
  `Decomp/Analysis/exports/WildStar64.exe/functions.csv`,
  `selected_reasons_summary.csv`, and `selected_decompiled.c` from run
  `housing_community_placement_privacy_labels`.
- Verification:
  `dotnet build Source\NexusForever.Network\NexusForever.Network.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-housing-plot-reservation\`
  and
  `dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-world-housing-plot-reservation\`
  passed. The follow-up scoped gates
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\world-server-housing-placement-privacy\`
  and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-housing-full-focused\
  --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingCommunityUpdateHandlerTests|FullyQualifiedName~ClientHousingCommunityRenameHandlerTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ClientHousingVisitResidenceHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"`
  passed `63/63`.

Housing community rename result opcode follow-up (`0x078C`):

- `0x078C` dispatch registration in `FUN_14006c290`
  (`WildStar64.exe:14006dba5`) points at the shared four-uint32 reader
  currently labelled `ServerSpellFourUInt32_ReadPayload`
  (`WildStar64.exe:14007fef0`). The reader writes four consecutive uint32
  fields into the packet object.
- The `CommunityRenameResult` event string at `WildStar64.exe:140afca10`
  traces to `Housing_HandleCommunityRenameResult`
  (`WildStar64.exe:1404bbfc0`). That consumer switches on the first uint32 at
  packet offset `0x10` and dispatches housing result cases for success, failed,
  invalid permissions, invalid residence name, insufficient funds, plus one
  still-unnamed enum value `0x38`.
- A targeted `DumpNearbyData` probe around the event descriptor passed to
  `ClientEvent_DispatchNamedEvent` shows `WildStar64.exe:1409ec1fc` is a
  single `i` descriptor. Retail build 16042 therefore exposes only the first
  uint32 result to the `CommunityRenameResult` UI event; the trailing three
  packet uint32 fields are read from the wire but unused by this event path.
- NexusForever now models `0x078C` as
  `ServerHousingCommunityRenameResult`, writes `HousingResult` as the first
  uint32, and keeps the remaining three uint32 fields as reserved zeroes in the
  current server emitter.
- Existing `ServerHousingCommunityRename` (`0x024D`) is intentionally left
  distinct: its native reader (`WildStar64.exe:14009f030`) reads target
  residence identity plus a 7-bit result-like field, while `0x078C` is the
  four-uint32 `CommunityRenameResult` event payload.
- Labels now appear in
  `Decomp/Analysis/exports/WildStar64.exe/functions.csv`,
  `selected_reasons_summary.csv`, and `selected_decompiled.c` from run
  `housing_community_rename_result_labels`.
- Verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\world-server-housing-rename-result\`
  passed, and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-housing-rename-result\
  --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingCommunityRenameHandlerTests|FullyQualifiedName~PacketPlaceholderNamingTests"`
  passed `47/47`; the combined housing focused gate passed `55/55`, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-full-housing-rename-result\`
  passed `625/625`.

Housing interior wallpaper implementation follow-up (`0x050D`):

- Existing labels now map `ClientHousingInteriorWallpaperUpdate_WritePayload`
  (`WildStar64.exe:14009ddd0`), `Housing_SendClientInteriorWallpaperUpdate`
  (`WildStar64.exe:1404b79d0`),
  `Housing_SendClientInteriorWallpaperRemove`
  (`WildStar64.exe:1404b7bc0`),
  `Lua_GameResidence_PurchaseInteriorWallpaper`
  (`WildStar64.exe:1406aca10`), and
  `Lua_GameResidence_RemoveInteriorWallpaper`
  (`WildStar64.exe:1406acc30`). The writer emits six uint32 existing-decor
  flags, then six shared `DecorInfo` records. The purchase binding reads six
  `HousingWallpaperInfo` handles and checks slot flags
  `0x01/0x04/0x08/0x10/0x20/0x80`; the remove binding restores one slot with
  default `HousingWallpaperInfo` id `5`. The sender fills only changed slots, uses
  `DecorType` value `3`, writes hook indices `1..6`, and carries the selected
  `HousingWallpaperInfo.Id` in `DecorInfoId`.
- NexusForever now names `DecorType.InteriorWallpaper = 3`, reads the six
  uint32 values as `ExistingDecorFlags`, validates changed slot hook indices
  and existing-decor flag consistency, resolves `DecorInfoId` through
  `HousingWallpaperInfo`, validates the slot flag/default id, aggregates and
  debits `Cost`/`CostCurrencyTypeId`, creates or updates the matching residence
  decor record with the raw wallpaper id, persists the native `DecorData`,
  `HookBagIndex`, `HookIndex`, and `ActivePropUnitId` fields on
  `residence_decor`, and broadcasts the changed rows through
  `ServerHousingResidenceDecor`.
- The DB migration `20260522153000_ResidenceDecorState` adds the native decor
  state columns needed for reconnect/load parity. Broader wallpaper ownership,
  unlock/prerequisite, refund, and inventory-collection semantics remain blocked
  until the corresponding server-side evidence is mapped.
- Verification:
  `dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-world-housing-interior-wallpaper\`
  and `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\world-server-housing-interior-wallpaper\`
  passed with `0` warnings and `0` errors.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-housing-interior-wallpaper\
  --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"`
  passed `59/59`.
  The broader focused F-004 gate with community update, rename, visit,
  neighbor, residence, packet-shape, and placeholder tests passed `68/68`.

Housing plug contribution payload follow-up:

- Native evidence:
  `ClientHousingPlugUpdate_WritePayload` (`WildStar64.exe:14009d560`) calls the
  contribution writer five times with `0x14`-byte records after the mapped
  identity, plot, plug, facing, reserved, and operation fields. The selected
  cached fragments for `Housing_SendClientPlugPlaceOrRotate`
  (`WildStar64.exe:1404b73e0`) and `Housing_SendClientPlugRepair`
  (`WildStar64.exe:1404b7620`) zero each record and write
  `HousingContributionInfo` offset `+8` into the first uint32 slot; the
  remaining four uint32 slots stay zero in the observed client senders.
- NexusForever now parses the five records as first-class
  `ClientHousingPlugUpdate.ContributionRecord` rows:
  `ContributionPointRequirement` plus four reserved uint32 fields. The runtime
  still treats any non-zero contribution record as unsupported and returns the
  existing conservative `Plug_CannotAfford` boundary for plug placement; repair
  remains `Plug_ModifyFailed` until plug damage/upkeep/resource semantics are
  mapped.
- Still blocked:
  contribution point spending, item-tier/resource contribution matching,
  build timers, and upkeep/repair state remain non-mutating. The parsed
  reserved contribution fields should stay diagnostic-only until a client
  consumer or retail capture proves their meaning.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-housing-plug-contribution-broad\
  --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingCommunityUpdateHandlerTests|FullyQualifiedName~ClientHousingCommunityRenameHandlerTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ClientHousingVisitResidenceHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"`
  passed `70/70`.

Housing early output, basics follow-up, and vendor-list remap:

- `TraceFunctionCallers.java` for `ServerHousingResidenceKeyedUpdate_ReadPayload`
  and `InspectCodeAddress.java` on `FUN_14006c290` confirmed the registration
  rows for the remaining F-004 early-output cluster. The registration sizes are
  client packet-object sizes, not guaranteed wire byte counts.
- `ServerHousingResidenceKeyedUpdate_ReadPayload` (`WildStar64.exe:14008de20`)
  reads only a uint64 field and one uint32 field. NexusForever now writes only
  those two wire fields; the previously modeled trailing uint32 was object
  padding/reserved space in the client registration.
- `0x00CB`, `0x00D1`, and `0x010D` register the shared
  `ServerEmpty_ReadPayload` and now emit no payload bits. `0x00CC` and
  `0x00CD` register the 15-bit scalar stub at `14007c3a0`; `0x00CE` uses the
  shared `NetworkBitReader_ReadWideString`. NexusForever models those shapes as
  empty payloads, 15-bit scalar payloads, and a wide-string payload while
  keeping their meanings provisional.
- `ServerHousingBasics_ReadPayload` (`WildStar64.exe:14008e610`) confirms the
  existing `0x010E` model as two uint64 fields plus one uint32 privacy/status
  flags field. `ServerHousingBasicsFollowup_ReadPayload`
  (`WildStar64.exe:14008de70`) maps `0x0110` as uint32, 18-bit scalar, uint32,
  and 8-bit scalar; NexusForever now exposes those fields diagnostically in
  `Server0x0110`.
- `ServerHousingVendorList_ReadPayload` (`WildStar64.exe:14009e7f0`) reads a
  uint32 count, count `0x18`-byte vendor rows, and a 2-bit list type. The row
  reader (`WildStar64.exe:14008bf00`) is uint64 source id plus three uint32
  fields: plug item id, cost, and plug item flags. This confirms the existing
  `ServerHousingVendorList` row shape and adds focused packet coverage; exact
  list variants and ownership/entitlement pricing behavior remain blocked.
- Evidence logs:
  `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.TraceFunctionCallers.ghidra.log`,
  `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_14007c3a0/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_140080d20/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_14008de70/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_14008e610/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_14009e7f0/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`,
  and
  `Decomp/Analysis/logs/runs/inspect_14008bf00/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`.
- Verification:
  `dotnet build Source\NexusForever.Network\NexusForever.Network.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-f004-wire-shapes-seq\`
  and
  `dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-world-f004-wire-shapes\`
  passed. Focused packet test execution is still blocked by unrelated dirty
  `AccountTerminalHandlerTests` compile errors around
  `RecordingDispatchProxy<IWorldSession>`/`RecordingDispatchProxy<IGameSession>`
  and missing `VerifyInvocation`.

Housing F-004 closure sweep:

- Implemented from existing mapped surfaces:
  pending neighbor invite responses now distinguish an expired invite from a
  missing invite. `ResidenceManager.TryTakePendingNeighborInvite` consumes the
  pending invite and reports whether it had crossed `ExpiresAt`; the response
  handler now emits `HousingResult.Neighbor_RequestTimedOut` for the expired
  lifecycle instead of collapsing it into `Neighbor_NoPendingInvite`.
- `ClientHousingReturnHandler` now uses `GetOrCreateResidence()` before
  teleporting home from another residence map. This matches the existing
  visit/return fallback that creates the player's residence when needed and
  avoids a null residence edge while preserving the invalid-packet guard for
  non-residence maps and already-home maps.
- Decor and interior wallpaper purchases now conservatively reject unsupported
  prerequisite/unlock rows with `Decor_PrereqNotMet`. `HousingDecorInfo`
  `PrerequisiteIdUnlock` and `HousingWallpaperInfo` `PrerequisiteIdUnlock`,
  `PrerequisiteIdUse`, and `AccountItemIdUpsell` are treated as blocked
  runtime prerequisites until NexusForever has a verified unlock/entitlement
  store for housing visuals.
- Still blocked:
  exact roommate/neighbor invite duration and UI timeout broadcast timing;
  eviction notifications to online removed neighbors; roommate permission
  semantics beyond the mapped 0..2 row value; community donation resource,
  contribution-point, item-tier, and refund behavior; full decor/plug ownership
  inventory and entitlement checks; housing vendor list variants beyond mapped
  plug rows; retail visit session/return state and privacy edge cases beyond
  current public/private/neighbors/roommate checks; interior wallpaper refund
  and inventory collection; `Server0x00CB..00D1`, `0x010D`, and `0x0110`
  consumer intent beyond their now-mapped wire shapes; and the remaining
  provisional neighborhood/community field names. These remain evidence-blocked
  rather than safe to mutate.
- Verification:
  `dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-world-f004-closure\`
  passed. `dotnet build` also passed for `NexusForever.Game.Abstract.csproj`
  and `NexusForever.Game.Tests.csproj` with `--no-dependencies`, covering the
  touched interface and test source. The broader focused housing test command
  was attempted but did not compile because unrelated dirty account/storefront
  work currently references missing or mismatched
  `AccountPendingItem`/storefront symbols in `Source\NexusForever.Game`,
  `Source\NexusForever.Database.Auth`, and `Source\NexusForever.WorldServer`.

Housing F-004 blocked-evidence tightening and wallpaper restore follow-up:

- `InspectCodeAddress.java` confirms `Housing_SendClientInteriorWallpaperUpdate`
  (`WildStar64.exe:1404b79d0`) suppresses no-op slots: it sends `0x050D` only
  when at least one requested `HousingWallpaperInfo.Id` differs from the current
  client state. `Lua_GameResidence_RemoveInteriorWallpaper`
  (`WildStar64.exe:1406acc30`) calls `Housing_SendClientInteriorWallpaperRemove`
  (`WildStar64.exe:1404b7bc0`) for slot indices `1..6`; that sender fills one
  changed `DecorInfo` row with `DecorType=3`, the slot hook index, existing
  decor flag `1`, and default `HousingWallpaperInfo` id `5` as `DecorInfoId`.
- NexusForever now treats default interior wallpaper id `5` as valid for every
  interior slot and skips currency debit for that default restore path. Focused
  test coverage asserts that a restore request for slot six with a costly table
  row id `5` updates the decor id without calling `CanAfford` or
  `CurrencySubtractAmount`.
- `Housing_HandleNeighborInvitePrompt` (`WildStar64.exe:1404bba70`) only
  dispatches `HousingNeighborInviteRecieved` and caches inviter identity/name
  for the response flow; the accept/decline callbacks (`1404ba070`/`1404ba0a0`)
  send only the mapped `0x0513` boolean. No client-side timeout duration,
  eviction notification fanout, or permission-state mutation timing surfaced in
  this pass.
- `Housing_SendClientPlugPlaceOrRotate`/`Housing_SendClientPlugRepair` still
  only expose `HousingContributionInfo.ContributionPointRequirement` in the
  five 20-byte contribution records. The item-tier/resource ids and authoritative
  spending/refund mutation remain blocked on server/sniff/backing-store evidence.
- Evidence logs:
  `Decomp/Analysis/logs/runs/inspect_housing_wallpaper_purchase_lua/WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_housing_wallpaper_send_remove/WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_housing_wallpaper_remove_send/WildStar64.InspectCodeAddress.ghidra.log`,
  `Decomp/Analysis/logs/runs/inspect_housing_wallpaper_remove_lua/WildStar64.InspectCodeAddress.ghidra.log`,
  and
  `Decomp/Analysis/logs/runs/inspect_housing_decor_info_writer/WildStar64.InspectCodeAddress.ghidra.log`.
- Verification:
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj
  --no-restore --no-dependencies -v minimal --nologo
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-f004-wallpaper-default\`
  passed with `0` warnings and `0` errors, and
  `dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj
  --no-restore -v minimal --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\network-world-f004-wallpaper-default\`
  passed with `0` warnings and `0` errors. The focused wallpaper test passed
  `1/1` using
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  -p:DefaultItemExcludes=Account\Reward\RewardRotationEntryStateBuilderTests.cs
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\game-tests-f004-wallpaper-default-filtered\
  --filter "FullyQualifiedName~ResidenceMapInstanceInteriorWallpaperTests"`;
  this targeted exclude emitted CS2002 duplicate-source warnings but completed
  successfully.
  The unfiltered test project compile is currently blocked by unrelated dirty
  `RewardRotationEntryStateBuilderTests.cs` references to a missing
  `RewardRotationEntryStateBuilder`.
- Still blocked:
  exact invite timeout duration/broadcast policy, eviction/permission update
  fanout to online neighbors, community donation contribution/resource spending,
  decor/plug entitlement inventory, vendor ownership/list variants, retail visit
  session and return-state persistence, non-default wallpaper refunds/inventory
  collection, unresolved housing output consumer intent, and remaining
  provisional neighborhood/community field names.

Housing F-004 pass 2 blocked-evidence sweep (eviction fanout, invite timeout
broadcast, donation debits, vendor purchase debits):

- **BLOCKED - evicted-player online notify:** Native consumer
  `Housing_HandleNeighborUpdate` (`WildStar64.exe:1404bb790`) handles inbound
  `ServerHousingNeighborUpdate` (`0x0519`) for the local client only. Action
  `2` removes a row from the local neighbor caches and dispatches
  `HousingNeighborsLoaded`; action `0` adds and dispatches `HousingNeighborUpdate`.
  The eviction send path is client-to-server only:
  `HousingEvent_SendClientNeighborEvict` (`1404b9f10`) and
  `Lua_HousingLib_NeighborEvict` (`140735fb0`) emit `ClientHousingNeighborEvict`
  (`0x0515`) with the selected neighbor residence identity and do not open a
  second outbound notify channel. No WildStar64 export/label shows the server
  proactively pushing `0x0519` Removed (or any alternate housing result/update)
  to the evicted player's session when another owner evicts them. NexusForever
  correctly updates the evictor (`ClientHousingNeighborEvictHandler` sends
  `ServerHousingNeighborUpdate` Removed to the acting session only); fanout to
  an online evicted neighbor stays blocked pending server/sniff evidence.
- **BLOCKED - proactive invite timeout broadcast:** Invite prompt consumer
  `Housing_HandleNeighborInvitePrompt` (`1404bba70`) caches inviter identity/name
  and dispatches `HousingNeighborInviteRecieved`; accept/decline callbacks
  `HousingEvent_SendClientNeighborInviteAccept` (`1404ba070`) and
  `HousingEvent_SendClientNeighborInviteDecline` (`1404ba0a0`) only send
  `ClientHousingNeighborInviteResponse` (`0x0513`) with boolean `1`/`0`. No
  housing-neighbor label or `HousingResult_*` string maps a client-side invite
  timer, `Neighbor_RequestTimedOut` auto-dispatch, or inviter timeout broadcast.
  NexusForever's `ResidenceManager.DefaultNeighborInviteExpirySeconds` (`300`)
  is emulator policy for expired invitee responses only; proactive inviter
  timeout packets remain blocked.
- **BLOCKED - `HousingCommunityDonate` debit:** Client sender
  `Housing_SendClientCommunityDonate` (`1404b9ca0`) resolves
  `HousingDecorInfo`, rejects `Flags & 0x8`, and serialises one `DecorInfo` row
  through `ClientHousingCommunityDonate_WritePayload` (`14009da00`). Consumer
  `ServerHousingCommunityDonateUpdate_ReadPayload` (`14009e930`) reads paired
  uint32 source/target decor id arrays only. No currency, item, or
  contribution-point subtract call appears on the mapped donate path. Safe
  server mutation today is decor transfer + `0x04FE` id remap only.
- **BLOCKED - housing vendor plug purchase debit:** Vendor list reader
  `ServerHousingVendorList_ReadPayload` (`14009e7f0`) and row reader
  `ServerHousingVendorListRow_ReadPayload` (`14008bf00`) expose plug item id,
  cost, and flags for `0x0508`; placement still routes through
  `Housing_SendClientPlugPlaceOrRotate` (`1404b73e0`) on `ClientHousingPlugUpdate`
  (`0x0510`) with five `HousingContributionInfo`-derived 20-byte records where
  only `ContributionPointRequirement` is populated in the observed senders.
  No verified client helper debits currency/items on vendor-list receipt or
  plug-place alone; NexusForever lists vendor costs but keeps placement/repair
  contribution spending blocked (`ResidenceMapInstance.ValidateNewPlugPlacement`
  returns `Plug_CannotAfford` when contribution ids/payload are present).
- **Pass 2 decision:** Mapped-only / **Blocked** for all four pass-1 items; no
  NexusForever behavior changes in this pass.

Marketplace duration and mail template follow-up (F-005):

- **Mapped**: `Marketplace_SendClientAuctionSellOrderSubmit` @ `140519a00` sends only
  item guid (`param_1+0x40`), minimum bid (`+0x10`), and buyout (`+0x28`) on opcode
  `0x06DC`; no listing duration is transmitted.
- **Mapped**: `Marketplace_ItemAuctionDurationUiBind` @ `1406a1e80` loads
  `Game.ItemAuction` and reads `GameFormula` `0x420` (`1056`) tier seconds into
  `DAT_140c8b000..018` for UI tier selection; this does not reach the sell-order
  packet writer.
- **Mapped**: Item-auction expiry is server-side `GameFormula` `821`
  (`Dataint0` hours ? seconds, retail default 48h). Commodity orders carry
  `ListTime`/`ExpirationTime` `FILETIME` on `ClientCommoditySellOrderSubmit` and are
  normalized/validated against `1056` tiers.
- **Verified (retail parity)**: Auction-house item listings were fixed at 48 hours;
  players could not pick shorter or longer durations. That matches `0x06DC` carrying
  no duration field and NexusForever applying `821` only.
- **Mapped**: Mail templates for both `Game.ItemAuction` (`FUN_140433750`) and
  `Game.CommodityOrder` (`FUN_140433640`) use achievement-text id `0x3950`; won vs
  expired differentiation is via mail `ContentType`, not separate localized ids.
- **Rejected for item-auction post**: `GameFormula` `1080`/`1082` and player-selectable
  item listing durations are not on the mapped `0x06DC` path; `1056` UI bind
  (`1406a1e80`) does not change the sell-order wire format.
- Verification: `dotnet test ... --filter FullyQualifiedName~Marketplace` passed `9/9`.

F-003 LAS auxiliary server-output cluster follow-up (mapped wire shapes):

- Ghidra `FUN_14006c290` registration plus `InspectCodeAddress` on the reader bodies
  mapped six LAS-adjacent unresolved opcodes to typed `IWritable` models in
  `Source/NexusForever.Network.World/Message/Model/Abilities/ServerLasAuxPackets.cs`:
  `ServerLasOpeningUInt18` (`0x00B0`, `ServerUInt18_ReadPayload` @ `140080d30`),
  `ServerActionSetDualUInt32Lists` (`0x016B`, `FUN_14008dc30`: 4-bit + 32-bit +
  4-bit count + paired uint32 lists),
  `ServerActionSetUInt16` (`0x016D`, `LAB_14008ed80`: 16-bit scalar),
  `ServerActionSetUInt32` (`0x016E`, `ServerUInt32_ReadPayload`),
  `ServerActionSetUInt16List` (`0x019C`, `FUN_14008e1f0`: 7-bit count + ushort rows),
  and `ServerActionSetFiveUInt32` (`0x01A4`, `FUN_1400819b0`: five uint32 fields;
  same reader registered for `0x01AC`).
- Removed the old `Server0xNNNN` raw/placeholder classes from
  `ServerUnresolvedOutputPackets.cs` and renamed the enum entries in
  `GameMessageOpcode.cs`. Field semantics and emit sites remain blocked; this pass
  is wire-shape only.
- Verification:
  `dotnet build Source/NexusForever.Network.World/NexusForever.Network.World.csproj`
  succeeded with `0` errors.
  `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj
  --filter "FullyQualifiedName~ActionSetUnresolvedPacketShapeTests"` passed `6/6`.

F-017 damage/heal/shield/vital formula parity follow-up:

- Fixed the damage-family AP/SP missing-formula fallback so the client-observed
  `0.25f` value is used as a coefficient against `AssaultRating` or `SupportRating`,
  not as a replacement for the whole intermediate value.
- Fixed armor mitigation to apply the matching `DamageMitigationPctOffsetPhysical`,
  `DamageMitigationPctOffsetTech`, or `DamageMitigationPctOffsetMagic` property by
  damage type. Physical damage previously used the magic offset.
- Added focused `DamageCalculatorRetailParityTests` for the AP/SP fallback, typed
  mitigation offsets, mitigation clamp behavior, non-damage typed offsets, and
  missing armor formula handling.
- A read-only export inspection confirmed existing client anchors for
  `SpellDamageDescription_ReadPayload`, `ServerSpellEffectDamage_ReadPayload`, and
  `CombatLog_DispatchVitalModifierEvent`, plus unlabeled helper addresses that emit
  `CombatLogHeal`, `CombatLogDamage`, `CombatLogDamageShields`,
  `CombatLogAbsorption`, and `CombatLogHealingAbsorption`. These support packet/log
  mapping, but do not resolve distance/distribution splitting, HoT cadence,
  SapVital/ClampVital mode tables, shield/absorb packet timing, alias vitals, or
  broader side-effect parity.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  --filter "FullyQualifiedName~DamageCalculatorRetailParityTests"` passed `8/8`;
  the full `NexusForever.Game.Tests` project passed `760/760` with the same
  build settings.

F-017 absorption school-mask follow-up:

- Local client table evidence over `Spell4Effects.tbl.sql` shows 581 `Absorption`
  rows with `DataBits04` limited to `1`, `2`, `4`, `6`, and `7`. The distribution
  is `7:461`, `4:49`, `1:43`, `6:24`, and `2:4`, which matches a compact
  physical/tech/magic bitmask rather than the `DamageType` enum's ordinal values.
- Runtime absorption pools now preserve the decoded mask from `DataBits04` and
  consume only matching physical (`1`), tech (`2`), or magic (`4`) damage before
  normal shields. Non-school damage remains conservative and does not consume
  these school-masked pools until a fixture proves otherwise.
- Added client labels for `CombatLog_DispatchAbsorptionEvent`,
  `CombatLog_DispatchHealingAbsorptionEvent`, `CombatLog_DispatchDamageEvent`,
  `CombatLog_DispatchDamageShieldsEvent`, and `CombatLog_DispatchHealEvent`.
  `run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe
  -MaxDecompiledFunctions 900` completed successfully, and
  `Test-DecompileManifest.ps1 -Targets WildStar64.exe -FailOnMismatch` passed.
- Added `AbsorptionSemanticsTests` covering all observed masks and non-school
  damage. Focused verification with `AbsorptionSemanticsTests` plus
  `DamageCalculatorRetailParityTests` passed `27/27`.
- Remaining F-017 blockers are unchanged for distance/distribution splitting,
  HoT cadence, SapVital/ClampVital mode tables, alias vitals, shield/absorb packet
  parity, absorption stacking/refresh order, and broader retail rounding or
  weapon/item-budget formula details.

F-017 ClampVital health-ceiling mode follow-up:

- Local `Spell4Effects` evidence has 23 `ClampVital` rows across 23 spells. The
  ratio field remains `DataBits02` as a float-bitcast value, and the observed set
  is compact: `1.0`, `0.1`, `0.05`, `0.02`, `0.01`, `0.001`, plus the CQ Treasure
  staircase `0.9..0.2`.
- `DataBits00` clusters as `0/1`, `DataBits01` as `0/2`, and `DataBits04=1`
  appears on several reduction rows. The previous resolver treated
  `DataBits00=1` with `DataBits01=0` as invalid and treated `DataBits01=2` as
  `Vital.Breath`; this blocked the clearest health clamps such as Dead Realm
  `Spell4Id=75525`, Starmap `84383`, Black Hole `85458`, and Food Sickness
  `86766`.
- Runtime now treats all observed `ClampVital` rows as health-ceiling clamps and
  keeps non-health clamp behavior blocked. Added `ClampVitalSemanticsTests` to
  cover the observed mode combinations.
- Focused verification with `ClampVitalSemanticsTests`,
  `AbsorptionSemanticsTests`, and `DamageCalculatorRetailParityTests` passed
  `31/31`.

F-017 combat-log packet parity follow-up:

- The 900-function WildStar64 export confirms selected decompiled coverage for
  `CombatLog_DispatchDamageEvent`, `CombatLog_DispatchDamageShieldsEvent`,
  `CombatLog_DispatchHealEvent`, `CombatLog_DispatchAbsorptionEvent`, and
  `CombatLog_DispatchHealingAbsorptionEvent`. `functions.csv`,
  `selected_reasons_summary.csv`, and `selected_decompiled.c` all include those
  labels after the refreshed export.
- Normal damage effects now emit `CombatLogDamage` after `TakeDamage` updates
  kill state, using the existing damage description values for final health
  damage, raw damage, shield absorb, absorption, overkill, combat result, damage
  type, and effect type. Distance-dependent and distributed damage inherit the
  same conservative log path while their falloff/splitting semantics remain
  blocked.
- `CombatLogHealingAbsorption` now serializes the same cast context shape as
  absorption before/alongside `nAmount`, matching the client dispatcher's shared
  context helper. Both application logs and consumed-heal logs carry cast
  context.
- Added `CombatLogPacketShapeTests` for `CombatLogDamage`,
  `CombatLogHealingAbsorption`, `ServerSpellEffectDamage`, and embedded
  `ServerSpellGo` target damage descriptions.
- Focused verification with `CombatLogPacketShapeTests`,
  `ClampVitalSemanticsTests`, `AbsorptionSemanticsTests`, and
  `DamageCalculatorRetailParityTests` passed `35/35`.
- Still blocked: exact `ServerSpellEffectDamage` emit policy, raw combat-log read
  packet mapping, shield-heal interaction with healing absorption, shield-damage
  value semantics, HoT tick-only rows, dynamic target re-selection, and
  distance/distributed damage math.

F-017 damage result-state follow-up:

- `UnitEntity.TakeDamage` now derives final health-damage result state before
  after-apply proc probes: `KilledTarget` is set from the post-apply alive state
  and `OverkillAmount` is calculated from pre-hit health only when the hit
  killed the target. Delay-death consumption therefore remains non-killing and
  reports no overkill.
- Damage and shield-damage combat logs now set `bPeriodic` for bounded
  `TickTime`+`DurationTime` rows only, matching the already conservative spell
  scheduler boundary while leaving tick-only and duration-only rows unchanged.
- SapVital/alias-vital semantics were rechecked against the current decompile
  notes: the shared vital path already supports direct resources plus mapped
  class-resource aliases (`KineticCell`/`MedicCore`/`Volatility`,
  `StalkerA/B/C`, `SpellSurge`) and `Resource7`/dash. Remaining SapVital mode
  names and unsupported alias vitals stay blocked because the export still has
  enum/log-surface evidence but no direct gameplay consumer for those fields.
- Added `UnitEntityDamageResultTests` and extended
  `CombatLogPacketShapeTests`; focused verification for those tests passed
  `12/12`.

F-017 remaining-semantics mapped-only pass:

- Spawned focused read-only investigations for `ServerSpellEffectDamage` emit
  policy, shield/absorb/healing-absorption packet parity, and
  distance/distribution/formula semantics. All three stayed mapped-only:
  current evidence proves packet/log shapes and table presence, not a safe
  runtime widening rule.
- `ServerSpellEffectDamage` remains modeled and tested but not emitted by the
  generic spell runtime. The native reader labels prove the packet fields, while
  `SPELL_BROADCAST_ROADMAP.md` still requires correlated fixture evidence before
  enabling any send policy.
- Shield/heal/absorb combat-log packet shapes are pinned, but shield-heal versus
  healing-absorption consumption remains blocked; no current export evidence
  proves that anti-heal pools affect `HealShields`.
- Distance-dependent and distributed damage remain on the shared conservative
  damage path. Existing table anchors for `DistanceDamageModifier`, weapon power,
  and item budget do not yet map the runtime consumer, normalization point,
  target-count split order, or rounding position.
- Expanded `CombatLogPacketShapeTests` to include `CombatLogAbsorption`,
  `CombatLogDamageShield`, and `CombatLogHeal` payloads in addition to the
  existing damage, healing-absorption, standalone spell damage, and embedded
  SpellGo damage-description coverage.

F-025 entity auxiliary reader follow-up (mapped wire shapes):

- Ghidra `FUN_14006c290` registration plus focused `InspectCodeAddresses.java`
  passes mapped the remaining F-025-adjacent create/stat auxiliary server readers
  without changing runtime mutation:
  `ServerEntityCreateAuxRow_ReadPayload` @ `140095c20`,
  `ServerEntityCreateAuxRowList_ReadPayload` @ `140095ce0`,
  `ServerEntityCreateAuxBitPackedRowList_ReadPayload` @ `140095a80`,
  `ServerEntityCreateAuxBitPackedRow_ReadPayload` @ `1400959c0`, and
  `ServerEntityCreateAuxScalarList_ReadPayload` @ `140095b40`. The row shapes live in
  `ServerEntityCreateAuxPackets.cs` rather than fixed raw
  byte buffers. `0x0260` is a counted list of `0x025F` rows; `0x0261` is a
  counted list of `0x0263` rows.
- The unit-combat/entity-stat auxiliary cluster is now field-shape mapped at the
  reader level: `0x0889` shares the three-uint32 reader used by
  `ServerEntityThreatUpdate`; `ServerEntityStatUInt32UInt5UInt32_ReadPayload` @
  `140097620` reads uint32 + 5-bit value + uint32; `ServerUInt32WideString_ReadPayload` @
  `1400980f0` covers `0x08CC`; `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload` @
  `140097ee0` reads uint32 + 14-bit value + 18-bit value + wide string;
  `ServerEntityStatUInt32UInt5Pair_ReadPayload` @ `140097690` reads uint32 + 5-bit
  value + two uint32 fields; and `ServerEntityStatTwoUInt32UInt64_ReadPayload` @
  `140097f70` reads two uint32 fields plus uint64. Models:
  `ServerEntityStatAuxPackets.cs`.
  These shapes have packet tests, but names and emit policy remain blocked.
- `ServerEntityThreatListUpdate_ReadPayload` @ `140098080` now has a durable label
  matching the existing server model: source unit id, five threat unit ids, and
  five threat values. `ServerMapTrackedUnitUpdate` (`0x0849`) was rechecked
  against `FUN_1400a6c10`: tracked unit id, three 32-bit position components, and
  a 15-bit trailing value. A later consumer pass resolves that value as a
  `TrackingSlotId`, not a direct public-event objective id.
- Still blocked:
  semantic names and server send conditions for `025F/0260/0261/0263/0264`,
  `0889/08CC/08F4/0939/093D/093E`, `0x08A8` flag meanings, phase visibility mask
  sources, map-tracked-unit producer timing/objective binding, and deeper
  deferred-action/current-object/current-target CSI state. No runtime state
  mutation was added from adjacency alone.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  --filter "FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreatePacketTests|FullyQualifiedName~EntityVisualPacketShapeTests"`
  passed `56/56`; the full `NexusForever.Game.Tests` project passed `793/793`
  with the same build settings.

F-025 interaction/CSI semantics follow-up (implemented boundary, blocked producers):

- **Implemented**: `InteractionObjectiveUpdater` now has focused tests for the
  SucceedCSI crediting boundary that was previously only implicit in handler
  paths. Successful direct interaction, direct activate, and activate-cast flows
  credit `QuestObjectiveType.SucceedCSI` with the target `CreatureId`. Direct
  activate also credits `ActivateEntity`; activate-cast deliberately omits that
  credit while still granting SucceedCSI. Null entity inputs remain non-mutating.
- **Mapped**: `MapTrackedUnitUpdate_ApplyAndDispatch` @ `1403f4170` applies the
  decoded server update into client tracked-unit state and then calls
  `ClientEvent_MapTrackedUnitUpdate_Dispatch` @ `140430f80`, which builds the
  Lua-facing `MapTrackedUnitUpdate` event from a tracked-unit id and a table of
  three position components. `MapTrackedUnitDisable_ApplyAndDispatch` @
  `1403f4200` removes cached tracked-unit state and dispatches
  `MapTrackedUnitDisable` for a tracked-unit id.
- **Mapped**: `Lua_PublicEvent_GetTrackedUnits` @ `14068adb0` and
  `Lua_PublicEventObjective_GetTrackedUnits` @ `140690500` enumerate
  client-maintained tracked unit collections into `Game.Unit` Lua userdata, and
  `ClientDB_RegisterTrackingSlot` @ `1402426a0` registers
  `DB\TrackingSlot.tbl`. These correlate the public-event marker surface but do
  not prove the server producer timing or objective binding.
- **Blocked**: caller tracing for `140430f80` and `1403f4200` found data and
  callback-style references, not the authoritative server emit source. Do not
  auto-emit `ServerMapTrackedUnitUpdate` or `ServerMapTrackedUnitDisable` from
  objective progress/table adjacency until sniff evidence or a native consumer
  path maps send conditions and marker lifetime.
- **Blocked**: the opaque F-025 auxiliary opcodes
  `025F/0260/0261/0263/0264`, `08CC/08F4/0939/093D/093E`, exact `0x08A8` flag
  meanings, deeper deferred-action/current-object/current-target CSI state, CSI
  minigame/phase visibility, and full realm-bank move/sync packet parity remain
  non-mutating.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  --filter "FullyQualifiedName~InteractionObjectiveUpdaterTests|FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreatePacketTests|FullyQualifiedName~EntityVisualPacketShapeTests"`
  passed `60/60`. A full `NexusForever.Game.Tests` run compiled and ran but
  failed two unrelated untracked matching deserter spell tests:
  `RetailMatchingDeserterSpellsTests.GetPveDeserterSpell4Id_ScalesWithCompletion`
  at completion ratios `0.9` and `0`.

F-025 realm-bank and phase-visibility follow-up (implemented guard slice):

- **Implemented**: realm-bank moves stay on the already-mapped generic
  `ClientItemMove` item-location path. `Inventory.CanMoveItem` now rejects
  realm-bank source/destination moves unless the account has
  `SharedRealmBankUnlock`, enforces the entitlement capacity
  `16 + 8 * SharedRealmBankSlots`, and expands the local realm-bank bag when the
  entitlement capacity exceeds the default bag size.
- **Implemented**: realm-bank storage persistence now follows the item's final
  storage owner instead of leaving duplicate or stale rows. Items saved in
  `InventoryLocation.RealmBank` upsert `realm_bank_item` and remove any matching
  character `item` row; items moved out of realm bank upsert `item` and remove
  the matching `realm_bank_item` row. Realm-bank stack-count updates, same-bank
  moves/swaps, and cross-boundary swaps now call the same persistence boundary.
- **Mapped-only / blocked**: native evidence around `ItemMove_ValidateAndMaybeConfirmBindOnEquip`
  @ `1403c17d0`, interaction case `0x43` (`ShowRealmBank`), entitlement enum
  registration @ `1404e7f60`, and UI drag/drop paths supports generic item moves
  and entitlement gating, but does not prove a realm-bank-specific move opcode,
  exact numeric item-location enum value, or retail open/close/snapshot marker
  packet.
- **Mapped-only / blocked**: phase visibility remains packet-shape/default-mask
  evidence only. `ServerPhaseVisibilityWorldLocation` uses the shared
  `ServerTwoUInt32_ReadPayload` shape, and native Lua public-event map-region
  readers filter `WorldLocation2` rows by the local phase-mask fields, but no
  mapped native path ties those masks to `ServerEntityCreate`/destroy streaming.
  Do not add generic phase-aware `CanSeeEntity` gating until mask source,
  direction, and resync timing are proven.
- **Mapped-only / blocked**: the deeper CSI/deferred-action labels remain
  client-local availability and retry machinery. No additional server-owned CSI
  state was added beyond the SucceedCSI objective-credit boundary above.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  --filter "FullyQualifiedName~RealmBankInventoryTests|FullyQualifiedName~InteractionObjectiveUpdaterTests|FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreatePacketTests|FullyQualifiedName~EntityVisualPacketShapeTests"`
  passed `66/66`.

F-025 remaining semantic unblock pass (mapped-only plus packet refinements):

- **Mapped / implemented packet semantic rename**: `MapTrackedUnitUpdate_ApplyAndDispatch`
  @ `1403f4170` applies `ServerMapTrackedUnitUpdate` by caching the tracked-unit
  id, three position components, and payload `+0x10` into the client tracked-unit
  state before dispatching `MapTrackedUnitUpdate`. `Lua_GameLib_GetMapTrackedUnitData`
  @ `140511c80` takes that id, resolves a `TrackingSlot` row, and returns its
  `label` and `iconPath`. The server packet model now names the 15-bit field
  `TrackingSlotId`; objective binding is therefore `tracked unit id ->
  TrackingSlotId -> TrackingSlot.PublicEventObjectiveId`.
- **Mapped / blocked producer**: `MapTrackedUnitDisable_ApplyAndDispatch` @
  `1403f4200` removes the cached tracked-unit state and dispatches
  `MapTrackedUnitDisable`. Runtime emission is still blocked because no
  native/sniff evidence proves server `TrackedUnitId` allocation, update cadence,
  disable timing, or the rule for selecting a `TrackingSlotId` from public-event
  objective state.
- **Mapped-only / blocked visual-info producer**: `ServerEntityVisualInfoUpdate`
  (`0x08A8`) still parses unit id, 18-bit Creature2 id, 17-bit display info, and
  two one-bit flags. The flags now have full packet boundary coverage, but direct
  caller tracing found registration/reader evidence only. NexusForever continues
  to emit the existing full `ServerEntityVisualUpdate` path until consumer
  side effects and retail send cadence are proven.
- **Mapped-only / blocked realm-bank residuals**: `InteractionState_DispatchUiOpenClose`
  @ `1403a71f0` covers state `0x43` `ShowRealmBank`/`HideRealmBank`, while
  `ItemDragDrop_HandleGenericMovePaths` @ `1406d50f0` routes inventory drag/drop
  and split-stack paths through the generic item-move validator. The generic move
  and entitlement guard implementation remains the safe runtime boundary. Exact
  `InventoryLocation.RealmBank` numeric proof, converter mapping, and retail
  open/close/snapshot packet behavior remain blocked.
- **Mapped-only / blocked opaque auxiliary opcodes**: fresh direct caller checks
  for `025F/0260/0261/0263/0264` and `08CC/08F4/0939/093D/093E` found only
  registrations, list wrappers, shared readers, or generic opcode lookups. Keep
  their neutral decoded packet models and do not emit them until semantic
  producers/consumers or sniff evidence are available.

F-006 account/storefront terminal cluster `0969..0991` unblock pass:

- **`0x0986` / `0x098F` (blocked, no live emit)**: `FindImmediateInstructions` for `0x986`
  still finds only `FUN_14006c290` registration with `ServerUInt64_ReadPayload`.
  `Storefront_HandleServer0987To0991` (`14044b630`) excludes `0x0986`; `0x098F`
  falls through with no client event. NexusForever keeps diagnostic models only and
  does not enqueue either opcode in runtime paths.
- **`0x096A..0x096C` leading uint32 (verified unused)**: handlers
  `AccountItemAddToCache_HandleServer096A` (`140004e30`),
  `AccountItemListAppend_HandleServer096B` (`140004f60`), and
  `AccountItemRemoveFromCache_HandleServer096C` (`140005040`) do not read the
  leading field. Server emits use `UnusedLeadingField = 0`; producer semantics
  for non-zero values remain unmapped.
- **`0x097A` CREDD order-cache rows (partially verified)**: reader
  `ServerCREDDExchangeOrderCacheRows_ReadPayload` (`1400a0210`) and appender
  `CREDDExchangeInfo_AppendOrderCacheRows` (`140007760`) prove compact `0x10`
  rows. Runtime maps `OrderId` (uint64), `CreditAmount` (14-bit), and `SideFlag`
  (7-bit buy/sell) for transient/durable order book refresh; no client event.
- **`0x026A` owned-order rows (blocked)**: `CREDDExchangeInfo_BuildLuaResults`
  (`14042b9a0`) proves the fixed `0x50` header (counts, three buy/sell price
  buckets, owned-order count). Non-zero owned-order pointer tail bytes remain
  blocked; emulator emits header-only snapshots plus populated buckets from the
  in-memory/DB order book without inventing tail rows.
- **`0x0790` coupon request (wire verified, sender blocked)**: model
  `ClientAccountRedeemCoupon` (wide string) and handler path are implemented with
  hardcoded emulator codes. No durable native sender label for opcode `0x0790`
  was found in exports; enum-only `RedeemCoupon` / `InvalidCoupon` evidence
  remains the policy boundary for retail catalog expansion.
- **`0x098C` / `0x098D` variant (verified routing)**: paired client senders
  `Storefront_SendClientPurchaseCharacterOffer` (`140450720`, `0x082A`) and
  `Storefront_SendClientPurchaseAccountOffer` (`1404507e0`, `0x0828`) prove the
  opcode split even though `Storefront_HandleStorePurchaseOfferResult`
  (`14044c780`) dispatches the same `StorePurchaseOfferResult` event for both.
  WorldServer now emits `ServerStorePurchaseOfferResult` / `ServerStorePurchaseOfferResultVariant`
  on successful storefront purchases.
- Migrations `20260522120000_AccountPendingItem`, `20260522130000_AccountCREDDExchange`,
  and `20260522140000_AccountDailyLoginAndStoreHistory` are present for pending
  items, CREDD orders/history, daily login, and store purchase history.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~AccountItemCooldownTests|FullyQualifiedName~CREDDExchangeHandlerTests|FullyQualifiedName~StorefrontPurchaseHandlerTests|FullyQualifiedName~AccountInventoryPendingGroupTests|FullyQualifiedName~AccountItemHandlerTests|FullyQualifiedName~AccountRuntimeEvidenceTests|FullyQualifiedName~AccountTerminalHandlerTests|FullyQualifiedName~VirtualCurrencyPackageHandlerTests|FullyQualifiedName~StorePurchaseVelocityLimiterTests"
  -m:1 -v minimal --nologo -p:UseSharedCompilation=false`
  (focused F-006 cluster).

F-003 crafting/support/realm auxiliary opcode decode (2026-05-22, second unblock pass):

- Ghidra `FUN_14006c290` registration plus `FindImmediateInstructions` / `InspectCodeAddress`
  mapped wire shapes for `0x084B`, `0x0855`, `0x05A1`, and support cluster `0x0347..0x0351`
  (including `0x0349`, `0x034A`, `0x034B`, `0x034F`, `0x0350` newly added to
  `GameMessageOpcode`).
- Typed `IWritable` models now replace `ServerUnresolvedRawPayload` placeholders:
  `ServerCraftingAuxSixUInt32`, `ServerCraftingAuxThreeUInt32`, `ServerRealmAuxUInt32TripletList`,
  and `ServerSupport*` models under `Message/Model/Support/`.
- Shared readers reused: `ServerUInt32WideString_ReadPayload` (`1400980f0`) for `0x0347`,
  `ServerUInt32_ReadPayload` (`14007ab50`) for `0x034A`/`0x0350`,
  `ServerEmpty_ReadPayload` (`14007d8e0`) for `0x0349`/`0x034B`.
- No live NexusForever emitters reference these opcodes; emitters intentionally omitted.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  --filter "FullyQualifiedName~CraftingPacketShapeTests|FullyQualifiedName~SupportPacketShapeTests"`
  (packet-shape tests).

F-016..F-020 spell-runtime family evidence ladder (2026-05-22):

- **F-016 Procs / Implemented boundary**: fixture `Spell4=4046` (Brutal Damage Proc)
  confirms event `12`, trigger `4047`, chance `0.15`, target data `4`.
  `ProcFixtureWitnessTests` pins the conservative dispatch boundary; next fixture
  `Spell4=35054` (heal-other capture path).
- **F-017 Damage/heal/shields / Mapped**: fixture `Spell4=82315` (Supercharged Shield
  Damage) decodes `DamageShields` coefficient `1.0`; distance/distribution and
  `ServerSpellEffectDamage` emit policy remain blocked. Next fixture `Spell4=32346`
  (HealShields).
- **F-018 CC/stacks / Mapped**: fixture `Spell4=57355` (Hivemind Trap) maps
  `CCStateSet.DataBits00=20` to `CCState.Tether`; stack-group arbitration remains
  blocked. Next fixture `Spell4=48757` (Stun).
- **F-019 Summons/traps / Implemented boundary**: fixture `Spell4=34094` (Stalker
  Proximity Mine) is gated by `SummonTrapEvidenceBoundary`; trap trigger firing
  remains blocked. Next fixture `Spell4=38860` (Tether Mine).
- **F-020 RavelSignal / Implemented boundary**: query `46` plus fixture `Spell4=76797`
  map mode `1` to `EntityScriptOnSignal` (signal `27096` matches paired `SetBusy`
  context); modes `2..5` stay diagnostics-only before any wider script side effects.
  Next fixture `Spell4=36188` (Tugga Context Busy signal spell).
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter Spell`

F-010 group/raid/queue cluster opcode mapping (2026-05-22):

- Ghidra evidence: `Group_CopyMemberStatBlockFromPayload` @ `140607490` copies the
  member stat block used by roster/detail refresh handlers; `Group_HandleMemberAdd`
  @ `1406031d0`, `Group_HandleMemberRemove` @ `140603380`, and
  `Group_HandleReadyCheck` @ `140603970` consume group id plus member key fields.
  `Group_HandleMemberPromote` @ `140603450` dispatches `Group_MemberPromoted` (not
  ready-check status). Opcode-to-handler table at `140e22b18` is a data reference
  only; per-opcode proof remains size/field correlation plus existing NexusForever
  emitters.
- Typed server models now replace `ServerUnresolvedRawPayload` for
  `0x042A/0431/0436/0438/0441/045A/0461/0468/0718`. Mapped fields are pinned by
  `GroupPacketShapeTests`; trailing zero padding preserves retail payload lengths
  where mapped fields are shorter than the documented fixed sizes.
- Runtime emitters: kick and loot-rule validation results route through
  `GroupActionResultHandler`; role changes emit `ServerGroupMemberRoleChange`;
  join requests emit `ServerGroupRequestJoinWindow`; stat refresh also emits
  roster/detail packets; quest share responses emit `ServerQuestShareResult`.
- `Client0x062A/0634` remain single-`uint32` diagnostic models; native sender
  semantics are still blocked.
- `ServerRaidQueueStatus` is mapped structurally but has no live queue emitter yet.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false
  --filter FullyQualifiedName~Group` passed (`92/92`).

Server0x elimination follow-up (2026-05-23):

- Renamed the last `48` `Server0xNNNN` entries in `GameMessageOpcode.cs` to
  cluster/shape names (entity-create aux `0x025F`-`0x0264`, entity-stat aux
  `0x0889`/`0x08CC`/`0x08F4`/`0x0939`/`0x093D`/`0x093E`, item/options/path/chat/
  marketplace/public-event/story/realm/recruitment aux bands, and related
  single-byte or fixed-buffer packets). Spell (`0x0814`-`0x0819`) and housing
  (`0x00CB`-`0x00D1`, `0x010D`, `0x0110`) clusters were renamed in the same pass.
- Split models into `ServerEntityCreateAuxPackets.cs`, `ServerEntityStatAuxPackets.cs`,
  `ServerClusterAuxPackets.cs`, and `ServerHousingAuxPackets.cs`; removed duplicate
  placeholder classes from `ServerUnresolvedOutputPackets.cs` (bases and
  account/store packets remain there).
- `rg Server0x` over `Source/` is now **zero**. Opcode inventory refresh reports
  **699/699** server opcodes modeled with **no** placeholder-model queue.
- Field semantics and runtime emit policy for the renamed packets remain blocked
  per the evidence ladder; wire shapes are covered by
  `EntityAuxiliaryPacketShapeTests`, `PacketPlaceholderNamingTests`, and
  `SpellBroadcastPacketShapeTests`.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --filter "FullyQualifiedName~EntityAuxiliary|FullyQualifiedName~PacketPlaceholder|FullyQualifiedName~SpellBroadcast"`
  passed `51/51`.

Entity/cluster aux decompile follow-up (2026-05-23, consumer discovery):

- Added `Decomp/Analysis/ENTITY_AUX_DECODE_ROADMAP.md` with the **62** opcode inventory,
  registration object sizes (`0x025F` = `0x2C` row, `0x0263` = `0x24` row, list headers =
  `0x10`), and explicit **emit/semantics blocked** gates.
- `TraceFunctionCallers` on `ServerEntityCreateAuxRow_ReadPayload` (`140095c20`) finds only
  `ServerEntityCreateAuxRowList_ReadPayload`, `Network_RegisterServerOpcode_0351`, and data
  table pointers - no `HandleServer*` consumer yet. Confirms indirect dispatch, not missing
  labels on static call sites.
- Labeled `ServerUInt32Triple_ReadPayload` @ `1400a8a30` and
  `QueuedStateReplay_EnqueueUnitCreatedRecord` @ `1405cef50` (replay case `0x12`; not yet
  correlated to `0x025F`-`0x0264`).
- `ServerEntityStatUInt32UInt5UInt32_ReadPayload` @ `140097620` is registered for **`0x08F4`,
  `0x938`, and `0x93B`**; do not treat as a single gameplay packet.
- **Mapped (2026-05-23):** live post-read apply dispatcher =
  `WorldSocket_ProcessServerMessage` @ `140014f10`. `WorldSocket_Ctor` @ `14000a490` installs
  callback table `PTR_LAB_140b55100` at socket `+0x100` with filter `140014d30` @ `+0x08` and
  apply `140014f10` @ `+0x10`. Flow: `DAT_140c65808` `vtable+0x100` deserialize -> optional
  `AccountInventory_HandleServer0966To0980` / storefront fast paths -> linked handlers at
  `socket+0x14b0` via `vtable+0x58(handler, conn, opcode, parsedPayload)`. Pre-filter uses
  `vtable+0x50` in `WorldSocket_FilterServerMessageHandlers` @ `140014d30`. Example list
  insertion: `WorldZone_InsertUnitHandlerIntoList` @ `140356a30`. See
  `ENTITY_AUX_DECODE_ROADMAP.md` for the table and blocked next step (per-handler `+0x58`
  opcode maps for `0x025F`-`0x0264` and the **62** aux packets).
