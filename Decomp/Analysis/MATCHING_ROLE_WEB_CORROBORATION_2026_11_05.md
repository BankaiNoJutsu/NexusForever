# Matching role UI and enum corroboration - 5 November 2026

Status: authored UI/API names, ordinal roles and the C2S confirmation bit
conversion are corroborated. Incoming `0600` role text and server producer
policy retain their separate evidence gates.

This note follows [the new 0600 consumer mapping](NEXT_REA_UNBLOCK_TARGETS_2026_11_05.md).
It changes no Source, runs no shared REA/Ghidra session and launches no client.
The historical role-check/queue implementations and current diagnostic guards
are not reopened by source-name matches.

## Useful conclusions

- The authored role confirmation UI uses `MatchMakingLib.Roles` values in a
  table and passes it to `MatchingGameLib.ConfirmRole`. These are ordinal Lua
  role values, **not directly the wire bitmask**.
- The recovered registration assigns Tank0, Healer1 and DPS2. The pinned native
  follow-up independently verifies the registration and the C2S conversion
  `1 << ordinal`, yielding Tank1, Healer2 and DPS4 for role confirmation.
  This is not by itself a named-field proof for incoming `0600`.
- Authored raid controls use separate GroupLib role setters and member-flags
  events. Those are not independent proof of a `0600` sender or its timing.
- Neither the authored source nor the inspected public native excerpts provide
  an accepted `Client0x062A` or `Client0x0634` sender. One similarly numbered
  server-dispatch case was explicitly excluded.

## Original UI source and historical identity

The inspected mirror is `Zod-/Wildstar-Carbine-Addons`, revision
`aa608d0b837ff8037eb8509123d8e3ed4efcb9e4`, committed 12 September 2017.
Its files identify NCsoft-authored client UI; a third-party mirror is not a
publisher-authenticated final-build artifact. The last file-change commits are:

| File | Last change | Commit/version claim |
| --- | --- | --- |
| Live/MatchMaker/ConfirmRole.lua | 5 April 2017 | `4d3543b621c3a92c3e398d2390867967d5757755`, v1.7.0.15264 |
| Live/MatchMaker/MatchMaker.lua | 8 June 2017 | `8b5297088f58a1063d4c03e4447673bf3521d268`, v1.7.1.15481 |
| Live/RaidFrameBase/RaidFrameBase.lua | 16 November 2016 | `c1963cf22eaf0fcf618fbc773681b6ef206b8035`, v1.6.3.14640 |
| Live/RaidFrameLeaderOptions/RaidFrameLeaderOptions.lua | 29 July 2016 | `5492ad2de7a597abd951043722c3f568e677789b`, v1.5.4.13938 |

No inspected file identifies build16042. Source was read and hashed in memory;
the Lua files were not copied into the checkout.

### Role-check confirmation

ConfirmRole.lua registers `MatchingRoleCheckStarted` and
`MatchingRoleCheckCanceled`. Its buttons are keyed by the Tank/Healer/DPS
members of `MatchMakingLib.Roles`; `GetEligibleRoles` enables the available
choices. The selected table uses role values as both keys and values.
Acceptance calls `MatchingGameLib.ConfirmRole(self.tRoles)`; cancellation calls
`MatchingGameLib.DeclineRoleCheck`. At least one choice is required when roles
are requested. This is a client UI/API contract, not a rule fixing the
server-side group composition or a `0600` notification trigger.
[Authored ConfirmRole.lua, lines 73-171](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/MatchMaker/ConfirmRole.lua#L73).

The matching XML binds separate accept/cancel controls to those callbacks;
it supplies no packet IDs or numeric role enum values.
[Authored confirmation controls](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/MatchMaker/ConfirmRole.xml#L18).

### Queue/replacement APIs

MatchMaker.lua obtains eligibility from `MatchMakingLib.GetEligibleRoles`,
stores selected ordinal values in queue-option `arRoles`, then passes complete
option tables to `MatchMakingLib.Queue` or `QueueAsGroup`. Its replacement
buttons call `MatchingGameLib.LookForReplacements` and
`StopLookingForReplacements`. The source names `LeaveAllQueues` separately.
These are precise native-binding trace leads. They do not tie the otherwise
diagnostic `062A/0634` requests to these flows.
[Authored queue and replacement handlers](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/MatchMaker/MatchMaker.lua#L3166).

### Raid roles and localization keys are separate corroboration

RaidFrameBase maps the ordinal role names to the localization keys
`Matching_Role_Tank`, `Matching_Role_Healer` and `Matching_Role_Dps`. Its
self-role controls call GroupLib setters, and leader options use
`GroupLib.SetRoleTank`, `SetRoleHealer` and `SetRoleDPS` for selected members.
They observe member-flag changes. This establishes authored labels and a
separate group-role flow; no inspected source maps those labels to the numeric
localization IDs used by `1405c0f00`.
[Authored raid labels](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/RaidFrameBase/RaidFrameBase.lua#L32),
[leader role controls](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/RaidFrameLeaderOptions/RaidFrameLeaderOptions.lua#L135).

## Native ordinal registration and precise next targets

The public native excerpt `sub_14076ACB0` registers MatchMakingLib using the
method table at `140B764C0`. Its Roles table stores Lua numbers 0, 1 and 2 for
Tank, Healer and DPS. The apparent constants 4, 6 and 3 beside the three names
are their **string lengths**, not role values. The number assignments are the
separate double-precision values written to the Lua stack.
[Registration excerpt, lines 78-110](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_14076ACB0.c#L78).

The same public repository's `140768E00` registers MatchingGameLib using table
`140B76320`. Both public excerpts have unknown original executable identity;
the addresses are investigation leads until checked in the supported artifact.
The parent reports that the pinned `14076acb0` registration has now decompiled
successfully, independently corroborating the ordinal table.
[MatchingGameLib registration](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_140768E00.c#L47).

| Bounded native follow-up | Reason |
| --- | --- |
| MatchingGameLib `ConfirmRole` binding/callback | C2S conversion and filtered `05B2` sending are now mapped below; compare independently with the `0600` consumer |
| MatchingGameLib `DeclineRoleCheck` | `05B2` zero-role/false response is now mapped below; queue leave and replacement remain distinct |
| MatchMakingLib `GetEligibleRoles` | Corroborate returned ordinal values and class eligibility without assuming packet flags |
| MatchingGameLib replacement APIs | Reuse the known `05D5` positive control; do not relabel `062A/0634` from nearby roles or names |
| Numeric role text lookup | Recover accepted locale data or the pinned key-to-ID mapping before assigning names to the `0600` text branches |

## 0600 chat presentation: evidence boundary

The parent supplied the exact pinned consumer `1405c0f00`: a group identity
lookup precedes chat construction, and trailing low bits choose role-result
text. Its individual text IDs are `0x72A62` for bit1, `0x72A63` for bit2,
`0x72A64` for bit4, with combination IDs `0x72A65` through `0x72A68`.
Zero/nonzero messages use `0x4B787/0x4B788`. Those numbers are native
observations; the inspected authored Lua supplies labels, not their locale
mapping. No specific English sentence or role bit name is inferred here.

This consumer supersedes the old registration-only blocker for `0600`, while
server notification timing, recipients, replacement/backfill and authoritative
group-role mutation remain separate integration questions. The existing
[matching tracker](MATCHING_IMPLEMENTATION_STATUS.md) retains older checkpoint
entries and must be read with the newer native follow-through.

## Diagnostic request searches and exclusions

Authenticated exact-code searches for `0x62A` and `0x634` in the public native
repository returned no C2S sender. The only `0x634` hit was
`WorldPaketHandler.c`: a **server-to-client dispatch case** writing player
state. It is the wrong direction for `Client0x0634` and was not promoted.
[Excluded server dispatcher](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/WorldPaketHandler.c#L2280).

The older Hammster/NASADocs API material lists role/confirmation/replacement
names but supplies no accepted final-client sender or numeric locale corpus.
Its `GetSelectedRoles` remark about persisted choices is not a packet mapping.
[Historical API inventory](https://github.com/Hammster/wildstar-api-docs/blob/dd9a803184b6910eb352c9bee59bc28632a7f4b9/Classes/MatchingGame.md#L264).

Authored source checks covered ConfirmRole, MatchMaker, MatchTracker,
ContextMenuPlayer, GroupDisplay, RaidFrameBase and RaidFrameLeaderOptions.
GitHub code searches included the exact confirmation/replacement/API names,
numeric opcode spellings and role-localization keys. Search is bounded by
indexing and these repositories, not a claim that senders cannot exist.
No modern emulator implementation or blog was treated as retail evidence.

## Byte identities and validation

Hashes bind GitHub base64-decoded file bytes at the revisions named above;
they do not bind an original executable or a publisher signature.

| File | Bytes | SHA256 |
| --- | ---: | --- |
| ConfirmRole.lua | 5090 | `E2EFD033DAC35D413E88BA09BCDA08B8A23635200B3A3BCA501A2D4C5D50E065` |
| MatchMaker.lua | 161419 | `7E8637829D2F3C88369C0E832E42E1087CF170FFC776EC5DC329DC253CDD6DE7` |
| RaidFrameBase.lua | 46239 | `0965715A510DB38E39C6FD645DD6C7E9B9593DB2E0BB1AAD10C92D5A2B352AE9` |
| RaidFrameLeaderOptions.lua | 10444 | `9E614042B25EE85B10F7B5969089DE88FCA9291D69B8407B99E281A9FF084A6A` |
| sub_14076ACB0.c | 5654 | `5A00857B5385CA15E62016E0E7A91D3AF250CCFAAF58A8EB07CBB6C9BCB4F6D4` |

The web reader timed out opening the individual GitHub pages; direct read-only
GitHub API/raw requests succeeded. File histories, byte identities, source line
anchors, local links and whitespace were checked. No Source changed, so this
note claims no application build or test result. Native launch, Computer Use,
commit and push were not performed.

## Pinned role-confirmation corroboration

The parent native pass reread the matching executable with SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
This note inspected its retained outputs under
`artifacts/continuation-20261105/deeper-roadmap/` without opening or changing
the shared native session. The parent aggregate is
`DEEPER_ROADMAP_CONTRACTS_2026_11_05.md`.

| Binding/consumer | Pinned relationship |
| --- | --- |
| Roles registration `14076acb0` | Tank0, Healer1 and DPS2 are native Lua numeric values; string data at `140b4e99c` and `1409f981c` identify Tank/DPS |
| ConfirmRole | Binding cell `140b76370`, name at `140b4e538`, callback `140769620` |
| DeclineRoleCheck | Binding cell `140b76380`, name at `140b4e520`, callback `1407697a0` |

The decompiler output for ConfirmRole omits essential mask arithmetic. The
complete LLVM instructions at `140769738-140769751` show the actual operation:
failed parses are skipped; ordinal values outside the unsigned range0..2 are
skipped; each remaining value produces `1 << ordinal`, ORed into the accumulated
mask. The accumulator is then passed to `1405c36e0`. The ordinal names and
observed shift independently give Tank1, Healer2 and DPS4 **on this C2S path**.
Do not describe the incomplete pseudocode alone as establishing this conversion.

`1405c36e0` applies a class eligibility mask before sending `05B2`:

| Native class ID | Eligible mask |
| --- | --- |
| 1, 2, 5 | 5, retaining bits1/4 |
| 3, 4, 7 | 6, retaining bits2/4 |
| Other/default | 0 |

The request contains the matching context at manager `+0x110`, the filtered
roles, and accepted=true. The decline callback uses the same context with
roles0 and accepted=false. These are static native send paths; no live
role-confirmation session was run. They do not prove that a server must accept
an empty filtered mask, nor do they establish a fixed group-role composition.

Retained evidence inspected:

- `housing-role-console.log`: pinned registration and Lua ordinal values.
- `role-craft-api-console.log`: ConfirmRole/DeclineRoleCheck callback bodies,
  including the explicit decline payload; incomplete ConfirmRole pseudocode is
  retained rather than silently repaired.
- `service-role-helpers-console.log`: class masks and `05B2` send fields.
- `confirm-role-llvm.txt`: complete mask loop and call-site instructions.

This resolves the previously open **outgoing** ordinal-to-mask conversion and
confirms distinct accepted/declined responses. Incoming `0600` still requires
an independent named-role/locale consumer connection before using these C2S
names for its trailing field. Numeric text IDs alone do not supply English
labels, and neither direction establishes public notification timing/backfill.

## Bounded local locale availability check

The historical parity record identifies installed clients at `D:/WildStar` and
`I:/Wildstar`. Exact `Patch/ClientDataEN.index` and `Patch/ClientData.index`
checks found no files at those checked roots. The read-only archive's expected
WorldServer Debug/Release `net10.0/en-US.bin` paths and the specific
`wildstar_client_mysql/LocalizedText.tbl.sql` path were absent. No
MySQL/MariaDB Windows service was reported. This does not exclude other
process-hosted, remote or differently located reference databases.

The retained LocaleInventory result contains historical index/candidate metadata,
not the missing numeric role text corpus. No broad disk scan or new locale
parser was run. Actual text for469602..469608 and309127/309128 remains
unrecovered in this check. Keep the authored localization keys and the native
numeric text IDs distinct until a real mapping is obtained.
