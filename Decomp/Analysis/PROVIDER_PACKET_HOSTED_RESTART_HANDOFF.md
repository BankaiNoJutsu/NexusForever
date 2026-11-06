# Provider packet timestamps and hosted restart — qualified continuation


## 6 November 2026 asset-owning-machine result

The current user resumed this continuation. The bounded timestamp and hosted
restart milestones below are complete; the overall restoration remains unfinished.
The following historical pause is retained for provenance, not as a current stop.
Computer Use and native-client launch remain stopped. No duplicate Goal was created.

Actual checkout: `I:/GIT/NexusForever-restoration-20261106`, branch
`codex/provider-packet-hosted-restart-20261106`, base `84483cc8` (includes
`b3fd74ab`). The supplied `I:/GIT/NexusForever` was the separate clean
`game_rework` checkout; it and the older assessment checkouts were preserved.
Canonical portable evidence is
[provider-packet-hosted-restart-20261106.json](coverage/provider-packet-hosted-restart-20261106.json).
Raw evidence remains in the new checkout under
`artifacts/provider-packet-hosted-restart-20261106/`.

### Timestamp hypothesis rejected at the tested boundary

The fixture omitted the real Guild's `IPlayerManager` recipient lookup. Its
committed publications also require `PublishMutationsAsync`, independently of
Character events. The fixture now supplies the directory, registers the loaded
member through `OnPlayerLogin`, and explicitly drains committed Guild messages.
Reloaded Guilds also receive the character directory needed by normal login.
Character initialization emits a separate Guild initial packet, so the Character
oracle selects the Character row rather than blindly taking the last packet.

All 16 new SQLite cases and the same 16 configured MySQL 8.0.46 probes pass; all 49
Character migrations apply per isolated MySQL fixture. The 118-case scoped Guild
fixture suite passes, including ownership, rollback and publication boundaries.
Winter, summer, the seconds on both sides of both Zurich clock changes, fractional
seconds and null completion are covered. Actual packet writers/readers verify the
update deletion flag/count, initial count and each 15/32/32/64-bit row field.

Coverage includes a provider-loaded real Guild row through another committed
update, fresh-owner initial packets, legacy row Build and Character committed
completion/fresh-owner packets. Progress and separate credited masks are preserved;
null remains zero and precommit Guild publication stays excluded. MySQL reads
have unspecified Kind; fractional `.8001234` stores at the next whole second in
this configured schema, and the wire matches that stored value. No production
conversion, database default, ownership guard or publication behavior changed.
The inactive six-case draft remains historical, outside active Source.

### Hosted replacement and prepared socket recovery

The discovered client is `D:/WildStar/Client64/WildStar64.exe`, version
`1.7.8.16042`, SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The retained cache at
`I:/GIT/NexusForever-assessment-report/remaining-goal/full-prepared-assets`
passes runtime table/map stream validation and all 1,211 inventory hashes
(386 table-directory files,825 maps), without an incomplete marker. No asset
was fabricated, regenerated or weakened for startup.

Incremental World/Auth and supporting API project builds were frozen with
private copied configs and seven new `nexus_assessment_20261106_restart_*`
schemas. All seven source schemas match independent post-run dumps exactly,
excluding only mysqldump's completion comment, including original entitlements.
The original MySQL80 service remains running. The private broker used a new
state directory/node/port 5686; original broker state/queues were not reset.

The documented owned Start-Process/Stop-Process mechanism was adapted to the
new frozen root, with exact executable, start-time and hash guards. Only World
34692/start 10:46:10.3909327Z and Auth 27972/start 10:46:10.3475675Z were replaced
with World 35628/start 10:51:22.4590536Z and Auth 36756/start 10:51:22.4393320Z.
All times are UTC on 6 November 2026. The seven other newly owned service
generations remained identical across that replacement. These PIDs are now
terminal evidence, never instructions to reuse them.

Passing protocol cycles include first-generation baseline and delivered-state
relog, then replacement-generation pending recovery, delivered control and a
second pending-account relog. Each requires actual ChangeWorld/PlayerCreate
bootstrap, ClientEnteredWorld acknowledgement, disconnect, offline Character
and closed Friendship presence for a newer Auth epoch. Six queues drain to
zero ready/unacknowledged messages. Hosting started alone is insufficient:
Auth initially returned NoRealmsAvailableAtThisTime before its 15-second realm
heartbeat. That attempt is retained and excluded; later full admission passes.

Recovery uses explicitly prepared accepted eligibility, actual 7194 metadata and
its six checklist rows, plus parent 7187. It does not claim raid/group earning.
Account 28/Character 75 carries intent
`g000000000000004b93f3cf4da9a6bcb` across replacement with no delivery, then receives
one `ServerAchievementUpdate` and its exact matching h-marker. Account 29/Character 76
retains delivered control `g000000000000004c3d8f5e2203f1e97`. Both remain
ProgressState 1, separate CreditedChecklistMask 0, null completion/wire zero.
Intent payloads/timestamps remain exact, replay adds no credit, and currency
is unchanged. The initial temporary control oracle incorrectly expected credited
mask 1; its failure is retained separately, and the corrected oracle agrees with
the existing actual-profile contract above. No production fix resulted.

### Identity, reviews, cleanup and next outcome

The portable record pins exact changed-test and relevant production sources,
DLLs and helper hashes. Complete local manifests cover 6,319 source/input files
and 1,264 frozen runtime files. Compiler PDB checksums match 1,018 Game, 460 World,
11 Auth and 725 test documents. All runtime files remain unchanged through the
cycles. Game.Tests builds retain the existing ASPIRE010 dependency warning;
there are no new xUnit analyzer warnings. World/Auth owning builds pass.

Separate Spec and Standards self-review passes found no open material findings;
these are primary-agent reviews, not independent reviewer claims. Raw review
and diagnostic records are retained. All 16 provider schemas and seven hosted
copies are independently absent. All 11 recorded service generations, owned
listeners, broker node and EPMD are stopped. The six new queues were empty
with zero consumers before normal broker shutdown and remain retained on disk.
Only temporary services created by this continuation were cleaned up.

The next evidence pass reconciled a stale prerequisite premise against the
installed hash: cached dispatcher 1404a2100 case 0x122 calls slot +0x3a8; direct PE
vtable 140b67740 read resolves it to 14049efa0, already registered in Source for
Unknown290 and UnderForcedMovement. Source still proxies ServerControl; actual
44606/raid eligibility is unqualified. Case 0xcb/slot +0x348 resolves to 140001ba0
(return 0), confirming Unknown203's client no-op. This does not establish a
server predicate or authorize a bypass/rename. REA verified PE/x86_64 identity
and was closed; no new native analysis engine or native execution was used.
Keep raid/group eligibility, all-member fanout, durable global-first, other
providers/routes and native UI/retail parity open. Reopen203 only with a
non-stub server/retail witness; do not repeatedly rediscover the client stub.

Git-only transfer still omits raw TRX/logs, private configs, SQL dumps, frozen
DLLs and temporary probe sources. Transfer the ignored evidence separately if
needed. The prior MySQL 51/SQLite 344/headless-restart qualification remains
retained and was reused, not rerun or relabeled as hosted proof.


Commands for the scoped source checks (from the qualified checkout):

```powershell
dotnet build Source/NexusForever.WorldServer/NexusForever.WorldServer.csproj --no-restore -v quiet --nologo
dotnet build Source/NexusForever.AuthServer/NexusForever.AuthServer.csproj --no-restore -v quiet --nologo
dotnet build Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v quiet --nologo
dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~GuildCreationFinanceTests
```

The retained temporary provider runner invokes the same test probes using
configured provider options and per-case migrated schemas. The hosted adapter
compiles the existing LoadTest protocol sources and adds only local fixture,
SQL, packet and lifecycle observations; its source and DLL hashes are pinned.
It reads private configuration in process, without connection strings on its
command line. No reusable production scenario or native earning producer was added.

## Historical machine-transfer checkpoint

6 November2026. The user explicitly stopped this continuation so it can resume
on the machine holding the runtime assets. This supersedes older active-goal
and historical runtime-process descriptions for this chat. The overall retail
restoration is unfinished. Computer Use and native-client launch remain stopped.
The [copyable resume prompt](RETAIL_RESTORATION_RESUME_PROMPT.md) is saved in the
repository. The publication preflight and its limits are recorded in
[restoration commit validation](RESTORATION_COMMIT_VALIDATION_2026_11_06.md).

## Resume objective

Finish configured-provider achievement packet timestamp qualification, then
qualify hosted World/Auth restart on the asset-owning machine. Preserve the
completed [MySQL/headless restart milestone](GUILD_CHARACTER_ACHIEVEMENT_ATOMICITY.md#configured-mysql-and-bounded-restart-qualification):
MySQL51, scoped SQLite240+104, six producer-loss windows/normal control, and
pending/delivered intent recovery across normal database restart. Do not repeat
or relabel those completed headless checks as hosted qualification.

Worktree at pause: `C:/GIT/NexusForever/continuation`, branch
`legacy-di-storefront-hardening`, observed HEAD
`96b4d1a66a428f48d01724538d4e74836ab5b6bb`. Other chats advanced this checkout;
preserve their committed and uncommitted work. No commit or push was performed
by this continuation. No production timestamp change was made.

## Timestamp investigation: hypothesis corrected, qualification unfinished

MySqlConnector's default provider reads use `DateTimeKind.Unspecified`.
However, `DateTime.ToFileTimeUtc` treats an unspecified value as UTC; it does
**not** apply a local-zone offset to that value. Thus the Guild builder's
existing call is not a demonstrated offset defect merely because Kind is
unspecified. Do not replace production conversion code on that hypothesis.

Primary sources checked6 November2026:

- [MySqlConnector DateTime storage](https://mysqlconnector.net/troubleshooting/datetime-storage/),
  default Kind on reads and storage limitations.
- [Microsoft DateTime.ToFileTimeUtc](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tofiletimeutc?view=net-10.0),
  unspecified values treated as UTC, local values converted to UTC.

The Guild's committed update/reload builder is in
`Source/NexusForever.Game/Achievement/GuildAchievementManager.cs`; legacy row
serialization is in `Achievement.cs`, and the Character committed builder is
in `CharacterAchievementManager.Publication.cs`. Wire achievement rows write
15 ID bits,32 progress bits,32 credited-mask bits and64 completion-time bits.

An owning Game.Tests build succeeded with the existing ASPIRE010 warning and
two draft xUnit analyzer warnings. Six draft SQLite cases cover null completion,
winter, summer, both clock-change dates and fractional seconds. All six failed
at the same fixture observation: no captured Guild600 update packet at draft
line54. They did not reach a failing timestamp comparison. This is not evidence
of a timestamp regression. Check actual Guild broadcast recipient registration
and committed publication draining before changing the oracle or production.

The unfinished test was moved out of `Source` to avoid adding six known failing
cases to the active source suite. A portable copy is tracked as
[GuildAchievementPacketTimestampTests.draft.cs.txt](continuation/GuildAchievementPacketTimestampTests.draft.cs.txt).
The original is retained locally as
`artifacts/continuation-20261106/provider-packet-hosted-restart/GuildAchievementPacketTimestampTests.draft.cs`,
SHA256 `60ea8bd1e43d7c97c266d8494b764824b48ef114fcb19dc76152aa32f2e5927b`.
Its build/test DLL may remain in shared Debug outputs; rebuild before deriving
new results. Do not treat these shared outputs as a qualified frozen candidate.

After repairing the fixture, exercise actual configured MySQL-loaded rows,
committed update publication, a newly loaded real Guild's initial packet and
legacy row Build. Add Character completed-row coverage and serialize/read the
actual wire fields. Compare with the **stored** SQL date, including provider
precision, rather than the original fractional actor value. Retain null=0,
progress/mask values and precommit publication exclusion. Keep production
unchanged if those checks agree. Fix analyzer guidance before restoring the
draft to the test project. Build/test only affected projects and scopes.

## Local shutdown and retained artifacts

Evidence root:
`artifacts/continuation-20261106/provider-packet-hosted-restart/`.

- `timestamp-build-01.log`: successful owning build; draft analyzer warnings.
- `timestamp-01.log`, `results/timestamp-01.trx`:6/6 fixture-observation failures.
- `server.json`, `start-owned-mysql.ps1`, `mysql*.log`: isolated startup using
  the previously verified private8.4.13 engine/datadir and loopback55184.
- `shutdown-verification.json`: PID42944, start
  `2026-11-06T09:46:28.7974798Z`, normal shutdown and absence of that generation,
  its listener and any mysqld using that datadir. No qualification schema was
  created in this segment. No World/Auth, broker or native process was launched.

All execution handles from this segment are terminal:58031(build),98835(tests).
Do not resume/poll/reuse them or start a second generation from a stale record.
The private engine and all earlier qualification generations are stopped.
Credentials remain private; never copy or print SecretPath contents. The draft
and [pause summary](coverage/provider-packet-hosted-restart-checkpoint-20261106.json)
are portable repository files. Full TRX/logs and frozen runtime binaries remain
local ignored artifacts; transfer them separately if needed. A Git-only transfer
does not include those raw records or the recovered tables/maps.

## Hosted World/Auth on the destination machine

The historical `I:/GIT/NexusForever-assessment-report` runtime, both expected
`.nexusforever-runtime` directories and a running NexusForever/MySQL/broker stack
were absent here. Verified384 tables plus English data exist under
`artifacts/continuation-20261106/client-data-recovery/recovered-tables`, but no
verified map cache was located. The user confirmed that continuation requires
the other machine. Do not manufacture map files or weaken asset validation to
claim a hosted/login result.

On the destination machine:

1. Read `AGENTS.md`, `Tools/Setup/README.md`, this note and the latest focused
   status. Locate and verify the existing build16042 table/map cache and private
   service configuration. Inventory actual owned processes, binary hashes,
   listeners, database routes and broker queues; historical PIDs are not current
   ownership. Preserve source edits, original rows, entitlements and defaults.
2. Finish the timestamp slice above using current source and isolated schemas.
   Retain exact source/DLL/provider identity and independent review evidence.
3. Prepare a frozen candidate with incremental WorldServer/AuthServer builds,
   avoiding concurrent writes to their outputs. Read
   `Tools/NexusForever.LoadTest/README.md` before using the existing
   `login-world` protocol scenario. Keep secrets out of reports and arguments
   where they can be exposed. Use only explicitly owned synthetic test accounts.
4. Restart only validated owned World/Auth generations through the documented
   runtime mechanism. Preserve other services, real data and broker queues;
   let queues drain normally. Require actual hosted readiness and complete
   authentication/world/bootstrap/disconnect/cleanup cycles before and after
   restart. Retain exact logs/row/receipt witnesses for the bounded state being
   qualified, including pending feedback and duplicate recovery when exercised.
5. Report hosted startup, protocol/socket cycles, achievement recovery and native
   behavior separately. A generic login does not prove live achievement earning
   or native receipt. Record cleanup/terminal process identities and update the
   canonical evidence/remaining-work records. Leave raid prerequisites290/203,
   all-member fanout, durable global-first and other retail gaps open.

Resume prompt: Continue retail restoration from this handoff on the machine
with verified runtime assets. Repair and qualify provider packet timestamp
observations first, then complete hosted World/Auth restart qualification.
Native client and Computer Use remain stopped. Preserve the completed MySQL51/
SQLite344/headless restart evidence and all unrelated work.

## Earlier parallel timestamp qualification (historical) — 6 November2026

The user resumed the broader Goal. The timestamp prerequisite now passes all12
configured Character/MySQL cases and166 scoped SQLite cases. Guild and Character
committed updates,fresh real-owner initial packets and legacy row Build agree
with the stored SQL date. Actual packet serialization/decoding checks15/32/32/64
bits,progress/mask and null=0 across winter,summer,both clock-change dates and
fractional completion.

The missing Guild packet was a fixture omission: GuildBase.Broadcast resolves
online members through IPlayerManager,which the finance fixture does not supply.
The new timestamp file supplies that registry for the existing real broadcast.
Production timestamp conversion is unchanged. On MySQL8.4.13 the fractional
03:04:05.8 input stores03:04:06 under the default profile; all three packet paths
publish that stored second with no seasonal offset. Loaded Kind is Unspecified.

The new [12-case test](../../Source/NexusForever.Game.Tests/Guild/GuildAchievementPacketTimestampBaselineTests.cs)
reuses the real Player/Guild/Character save and fresh reload paths. The
[portable result](coverage/achievement-packet-timestamps-20261106.json) pins its
Source,6310 published parent Source inputs,138 DLLs,12 provider observations,
49 exact migration identities and cleanup. Only Game.Tests differs from the
published Q3777 baseline; foreign Source is excluded. Owning test/adapter builds
are clean in16.33/2.88s; scoped SQLite execution reports2m29s and MySQL191.09s.
Both final reviews have zero findings. No full solution/full suite ran.

All12 synthetic schemas were dropped and independently confirmed absent. The
sole owned MySQL generation48028 and its listener are absent after normal
shutdown. Cold startup took about146s; the premature connection attempt, null
InlineData adapter discovery failure, initial generic/analyzer failure and
Character proxy-observation failure remain retained under
artifacts/continuation-20261106/achievement-packet-timestamps/.
These are corrected qualification/setup failures,not timestamp regressions.

This qualifies the current prepared Source/SQLite/Character-provider paths,
not hosted Auth,actual raid earning/all-member fanout,copied upgrades,other SQL
profiles,socket/native or retail behavior. The separate hosted World/Auth gate
still requires verified runtime assets and configuration. Native client/CUA stay
stopped. The paused draft and instructions below retain historical provenance;
do not rerun or restore the obsolete draft.
