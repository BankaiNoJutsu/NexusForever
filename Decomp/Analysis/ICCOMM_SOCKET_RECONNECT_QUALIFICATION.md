# ICComm socket and explicit reconnect qualification

6 November 2026. The frozen World/Auth candidate from `0fea0df75b9d17d2eda75a104d8024f7896743f0`
passes bounded Global and prepared-Guild ICComm socket scenarios. This extends
[membership ownership](ICCOMM_MEMBERSHIP_OWNERSHIP.md) with hosted protocol
evidence. No production server behavior changed in this milestone. F-012 and
the overall retail restoration remain partial.

## What passed

The final Global and Guild runs each completed seven STS/Auth/World sessions,
character selection, bootstrap and ClientEnteredWorld. Each retained 55 decoded
ICComm frames and 18 message attempts. Raw body hashes were independently checked.
The sender receives one matching result before its ordered echo; intended peers
receive one directed message with the captured sender name. The outsider receives
none of those payloads. Separate Unicode names retain different channel IDs;
repeated joins retain the active ID. An unaffiliated actor receives exact-name
NoGroup and NoGuild results.

ClientICCommChannelNotJoined removes only the selected membership. Its old sender
is denied, the other named channel still works, and explicit rejoin succeeds.
Normal and abortive-close requests both reach saved offline Character state and
closed Friendship presence. Reconnected peers receive new Auth World epochs and
cannot send or receive through the old membership until explicitly joining.
An active peer retains the same channel ID; after all members leave, an observed
world-tick reply precedes fresh joins with a new ID. Final SQL confirms unchanged
item/currency and entitlement hashes. Bob epochs are 13/14/15/16 for Global and
17/18/19/20 for Guild.

The abortive path disposes with `Linger(true, 0)` before cancelling its reader.
This is a requested abortive socket close: no packet capture or server reset-error
witness establishes TCP RST on the wire. Negative delivery checks wait 500 ms per
attempt; a second audit also finds no excluded payload through transcript end.
Neither observation proves unbounded absence or native automatic reconnect.

## Candidate, fixtures and checks

The dedicated checkout is `I:/GIT/NexusForever-iccomm-20261106`, branch
`codex/iccomm-membership-ownership-20261106`. All 1,211 retained build16042 cache
files match their asset inventory; current MapGenerator table/map validation
passes. Incremental project builds staged nine frozen services. WorldServer,
AuthServer and the harness build with zero warnings/errors; Game.Tests retains
the existing ASPIRE010 warning. All 66 scoped ICComm tests pass, including 16 new
wire-model and qualification-boundary cases and the prior ownership regressions.
No full solution or full suite ran.

Seven private `nexus_assessment_20261106_iccomm_*` schemas copied the originals.
Three newly seeded numbered Load accounts own Characters 75/76/77 and Accounts
28/29/30. The first two received ordinary membership in copied Guild 1, rank 9;
the third remained unaffiliated. This is prepared membership, not public guild
creation or positive Group qualification. No existing entitlement was edited.
The initial fixture's too-long Note was repaired before service startup; the
failed attempt is retained. Initial harness passes are retained separately from
the final harness, which also fails reports on cleanup errors, verifies result
ordering and disposes a connection if post-login ownership observation fails.

One separate startup-path defect is visible: CharacterListManager constructs a
presentation CostumeManager with a null Player, while its ownership guard
dereferences Player.CharacterId. Each selected fixture logs that exception in
character-list gear mapping; the catch path omits the character row, while the
list packet, SQL-ID selection and world entry still complete. Do not describe
these runs as an error-free character-list presentation
or use this issue to weaken the ICComm checks. Fix and qualify this separate
presentation/ownership boundary next. The subsequent
[Source-qualified fix](CHARACTER_LIST_COSTUME_PREVIEW.md) preserves the live
wardrobe guards; its changed World DLL needs separate hosted qualification.

The [portable record](coverage/iccomm-socket-reconnect-20261106.json) binds exact
source, frozen DLL/PDB, TRX, assets, reports, audit and cleanup identities.
Compiler checksums match 1,018 Game, 460 World, 727 test and 11 harness documents.
All 1,258 frozen runtime files remained unchanged. Final test runtime Game/World
DLLs match the hosted candidate. Separate primary-agent Spec and Standards
self-reviews have no open ICComm findings; no independent reviewer is claimed.

## Cleanup and continuation

All nine exact owned service generations were stopped after account cleanup.
Six broker queues had zero ready/unacknowledged messages before stop and zero
consumers afterward. The isolated broker node and its empty private EPMD were
stopped; owned listeners/processes are absent. All seven new schemas were dropped
under their lease guard and independently found absent. Before/after dumps of all
seven original schemas match after excluding only the dump completion comment.
MySQL80 remains running. Q3777 source/docs/coverage remain in the other worktree
untouched. Native-client launch and Computer Use remain stopped.

Raw evidence is ignored at
`I:/GIT/NexusForever-iccomm-20261106/artifacts/iccomm-socket-reconnect-20261106/`.
Transfer it separately, including frozen bins, private scripts/configs, fixture,
original dumps, transcripts and service/broker logs; it contains local secrets
and original data and must not be committed. Git carries the portable hashes and
reusable harness/tests. Relocate actual assets and generations on the next host.

Native addon callbacks/ready/left behavior, automatic reconnect, positive Group,
live affiliation changes, simultaneous socket replacement, cross-realm behavior,
circles, entitlement policy, quotas, throttling and persistent membership remain
open. Earlier in-process stale-owner regressions remain distinct from these
ordered socket disconnect/reconnect runs. Neither generic login nor this prepared
ICComm fixture proves achievement earning or retail parity.
