# Provider packet timestamps and hosted restart — paused for machine transfer

## Completed timestamp qualification — 6 November2026

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

The new [12-case test](../../Source/NexusForever.Game.Tests/Guild/GuildAchievementPacketTimestampTests.cs)
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
