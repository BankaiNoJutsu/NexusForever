# Friendship setting ownership

6 November 2026. `ClientFriendshipBlock` (`0x00B8`) now updates the character's
`CharacterFlag.FriendBlock`; `ClientFriendshipIgnoreStrangersState` (`0x039E`)
updates the existing Friendship account preference. Previously, the former
changed the account setting and the latter only echoed transient client state.
This is an implemented Source/SQLite boundary within partial F-012, not full
social-system or retail completion.

## Client and authored UI evidence

The build-16042 `WildStar64.exe` is 13,927,576 bytes with SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Portable REA PE import inspection establishes identity through evidence
`ev_3a002aa3d25d9d846a80c84d9d2d23eb73db439235129acc4a5501029628a320`.
Fresh PE pointer/byte inspection and LLVM disassembly supply the missing leaf
and dispatcher evidence; retained same-digest Ghidra fragments supply the
existing send/apply helpers. These are distinct provenances. No client execution,
native engine setup, Ghidra session, debugger or Computer Use was started.

| Binding | Observed native behavior |
| --- | --- |
| `SetLock`, registration pair `140b736e0` -> `14070e010` | Sends `0x00B8` with the requested boolean. Command helper `1405e39e0` also toggles bit 1 at global player `+0x6328`. |
| `IsLocked`, pair `140b736d0` -> `14070dfe0` | Reads exactly bit 1 of global player `+0x6328`; the 44-byte leaf is freshly disassembled. |
| `0x00FE`, dispatcher slot `1403f15b8` -> `1403ef871` | Writes the incoming flags to player `+0x6328` and dispatches `CharacterFlagsUpdated`. This agrees with existing `ServerCharacterFlagsUpdated` and `FriendBlock = 0x2`. |
| `SetPersonalIgnoreStrangersState`, pair `140b73750` -> `14070e5d0` | Compares/updates global player `+0x6ae0`, sends `0x039E` on change. This address is FriendMgr `+0x2a8`, distinct from character flags. |
| `0x03A5`, slot `1403f2054` -> `1403f0ac5` | Masks incoming flags with `2`, stores the account stranger state at `+0x6ae0`, dispatches `FriendshipAccountPersonalStatusUpdate`. |
| `0x03A6`, slot `1403f2058` -> `1403f0aaf` | Calls `1405e16c0` with FriendMgr at player `+0x6838`; copies personal strings, presence and stranger preference to its owned fields and dispatches the same event. |
| `GetPersonalStatus`, pair `140b73730` -> `14070e2a0` | Calls builder `1405e2d90`, exposing `bIgnoreStrangerInvites` from FriendMgr `+0x2a8`. |

The [NCsoft FriendsList Lua in an immutable addon mirror](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/FriendsList/FriendsList.lua)
corroborates the distinction: `OnBlockBtn` calls `SetLock`, while
`OnAccountBlockBtn` calls `SetPersonalIgnoreStrangersState`; account button state
comes from `GetPersonalStatus().bIgnoreStrangerInvites`. This authored UI is
API 16/version 15736, not independently proven to be the exact build-16042 Lua.
It corroborates the exact native bindings rather than replacing them.

The old account wording on label `1405e39e0` is corrected. Historical findings
using that wording are superseded by this address/field-level evidence.
The [portable coverage record](coverage/friendship-setting-ownership-20261106.json)
pins registration bytes, dispatcher entries, bounded native ranges, retained
exports, raw artifacts, changed source and validation assemblies. Raw client
assets, disassembly, Lua and local outputs stay outside Git.

## Integrated behavior and persistence

- Parsed `0x00B8` uses the original bound Player and existing `SetFlag`/`RemoveFlag`,
  preserving other character bits and publishing the existing `0x00FE` intent
  update. Its SQL save remains required; that immediate client update is not a
  database commit receipt.
- Character flags remain dirty until the exact staged flags actually flush and
  the Character transaction settles successfully. Rejection, rollback, unflushed
  commit, savepoint rollback and suppressed property writes retain retry intent.
  A version protects later requests, including a change back to the same flags,
  from acknowledgement by an older transaction. Enrollment/settlement uses the
  existing nonblocking map-admission gate and atomic dirty-mask operations.
- The Character API exposes nullable `BlocksFriendRequests`, mapped from the
  committed Character row. Friendship reads this owner for each new ordinary
  friend request, even when identity metadata is cached. Identity, realm and
  account must match. Both name and identity request handlers return
  `FriendsBlocked` before creating an invite when locked. A missing/old API
  field or foreign response returns `UnableToProcess`; HTTP failures cannot
  admit an invite. There is no duplicate Friendship flag column or migration.
- Deploy the updated Character API with Friendship. An old API without the
  nullable field cannot silently authorize a new ordinary friend invitation.
  This gate applies to new ordinary friend requests; existing acceptance logic,
  Rival/Ignore operations and account requests retain their existing validation.
  Decisions observe committed state at the API read, not a distributed lock
  across later character changes and Friendship commit.
- Parsed `0x039E` checks the original Player/account/session binding, then sends
  the existing account block command to Friendship. Its existing account setter
  stages state and personal-status outbox together. The service's normal save
  commits both; a refused flush persists neither, and a fresh retry succeeds.
  Existing account invitation validation consumes the saved preference.
- World queues the resulting `0x03A6` DTO on the captured event queue and checks
  the original Player/account/session epoch again on drain. It cannot follow a
  replaced player. It sends no speculative `0x03A5` echo before persistence.
  Existing `0x03A5` mask and `0x03A6` packet layouts are unchanged.

No automatic rewrite of existing account preferences is attempted: earlier
misrouted commands cannot reveal the user's intended setting retrospectively.
Auto-response messages remain transient pending their separate durable-owner
and readback evidence.

## Validation and review

Seven original focused cases failed against the published parent before the
implementation. The final focused run passes 29 cases (26 new and three existing
social-option cases). It includes parsed settings, full real Player saves,
fresh SQL reads, failure/retry, later intent, fresh API mapping/readback, both
real invitation handlers with durable pending rows, account request validation,
outbox refusal/retry, and queued readback after Player replacement.

The intermediate expanded run recorded an out-of-memory fixture initialization
failure and one incorrect test query of EF's tracked save stub. The query now
uses an independent SQL read; the serial build/focused rerun passes without
changing the runtime or relaxing its persistence assertion. Failing artifacts
are retained separately from final results.

The 195 related Source/SQLite checks pass, including the new cases, existing
Friendship presence/bootstrap, PvP cooldown, duel, transport and incoming chat
ownership controls. Final owning-build results are recorded in the coverage
JSON. No full solution build or full test suite was run. Final review covers
the request-to-save/readback behavior and the repository's ownership/persistence
conventions separately; remaining qualification limits are explicit below.

At this reconciliation boundary upstream #608 remains open at
`7db12d9a7c694bc5e66d9e0f4cdbc1b82155e69b`; #609 remains closed/unmerged at
`f9b6f7356798a41beea0768ff1f6b5a758ffeca3`. The refreshed creation search since
30 October contains those two proposals only. Unchanged reviewed diffs are
reused; no upstream merge or PR publication follows.

## Remaining gates

The later [account-command ownership follow-through](FRIENDSHIP_ACCOUNT_COMMAND_OWNERSHIP.md)
adds the recorded presence-generation fence to the four supported account
setting commands, with 74 new/188 related cases. It supersedes the missing
source-epoch/admission command guard below; it does not supply an ordered
within-admission command/readback revision or auto-response storage owner.

Configured MySQL, live broker delivery/reordering, within-admission command order,
correlation/replay policy, deployment/old-service transitions, process restart,
physical/native UI timing, two-client smoke and retail qualification remain
open. The account command/readback protocol has no new revision field, so an
older committed broker message can still arrive later. Original World actor
checks cannot establish a missing source revision. Auto-response durability,
wider friendship/public-flow coverage and full F-012 remain unfinished.
Continue independent integrated gameplay under the unified Goal; native client
launch and Computer Use remain stopped.
