# Resume retail restoration on the asset-owning machine

Saved for the requested machine transfer on 6 November 2026. Paste the following
into a chat in the destination checkout. This checkpoint supersedes older paused
draft instructions; the broader restoration is unfinished.

```text
Resume the NexusForever retail restoration for WildStar final client build 16042.
Start with the provider packet timestamp checkpoint, then hosted World/Auth restart
qualification. Resume the existing goal if available; create one only if none is
active, using Decomp/Analysis/RETAIL_RESTORATION_GOAL_PROMPT.md and this checkpoint.

Locate the actual checkout, branch and verified runtime assets. Do not assume the
previous C:/GIT/NexusForever/continuation path, historical I: drive, ports or PIDs.
Fetch and inspect origin/legacy-di-storefront-hardening before integrating it;
preserve dirty and concurrent work. Read AGENTS.md, CURRENT_STATUS.md,
Tools/Setup/README.md, Decomp/Analysis/ASSESSMENT_REMAINING_WORK.md,
Decomp/Analysis/FULL_MISSING_SYSTEM_RESTORATION_PLAN.md,
Decomp/Analysis/CONTINUATION_GUIDE.md and
Decomp/Analysis/PROVIDER_PACKET_HOSTED_RESTART_HANDOFF.md.

First reconcile provider packet timestamps with the current source:
- Published ae873085846f3145398ca8567a1923d4a4bbe61d completed 12 configured
  Character/MySQL cases and 166 scoped SQLite cases. Use the portable record
  Decomp/Analysis/coverage/achievement-packet-timestamps-20261106.json.
- Actual committed Guild/Character updates, fresh-owner initial packets and
  legacy Build serialization agree with stored SQL dates, including null,
  winter/summer, both clock-change dates and fractional provider rounding.
- The missing Guild packet was an omitted IPlayerManager fixture registry.
  Production conversion is unchanged. DateTime.ToFileTimeUtc treats Unspecified
  as UTC. Do not restore the obsolete six-case draft or invent a timestamp fix.
- Verify source/provider/profile applicability before reusing results. Re-run
  affected scoped checks only for meaningful drift or an unresolved concern.
  All 12 synthetic schemas and the owned MySQL generation/listener were removed.
- Earlier Guild-to-Character MySQL 51, SQLite 240+104 and producer/database-restart
  evidence remains separately pinned. Read its exact candidate limits; it does
  not qualify this combined destination build or hosted Auth/World.

Then qualify hosted World/Auth restart on this machine:
1. Locate and verify the build-16042 table/map cache or installation, private
   service configuration, actual database routes, broker and owned service
   generations. The previous machine had recovered tables/English data but no
   verified map cache. Git does not transfer ignored assets, secrets, raw logs
   or frozen binaries. Never manufacture assets or weaken asset validation.
2. Prepare an isolated, frozen candidate with incremental WorldServer/AuthServer
   project builds. Read Tools/NexusForever.LoadTest/README.md before protocol
   runs. Keep source, DLL, provider, configuration and asset identities together.
3. Establish actual hosted readiness and a complete authentication/world/
   bootstrap/disconnect/cleanup cycle. Restart only verified owned World/Auth
   generations through the documented mechanism. Preserve other services,
   original rows, entitlements and broker queues; allow normal draining.
4. Repeat the complete cycle after restart. Exercise pending achievement feedback
   and duplicate recovery where supported, retaining exact row, receipt, packet
   and lifecycle witnesses. Generic login does not prove achievement earning.
5. Record cleanup and terminal ownership checks. Report hosted startup, socket
   cycles, achievement recovery, native behavior and retail qualification as
   separate outcomes. Keep unexercised gates open.

Reuse the completed physical BOP binding and loot-feedback milestone recorded in
Decomp/Analysis/PHYSICAL_LOOT_DELIVERY_FEEDBACK.md and its portable coverage JSON.
The accepted whole award and collect transition precede fallible UI; retries do
not repeat quantity and stale map notices are abandoned. Its isolated owning
builds, 19 focused cases, 239 related cases and eight compiler-source bindings
pass. This is Source/SQLite evidence, not durable loot-receipt/process/provider/
socket/native qualification. Preserve the published Q3777, Q4526 and Q5597 slices
and all unrelated work; follow the latest canonical ledger after hosted checks.

Use REA for exact shipped-artifact questions and online search/digging for concrete
unanswered questions, prioritizing primary sources and retained evidence. Pin
build/hash identity and distinguish observed, correlated, mapped and verified
claims. Use Java only when a chosen analysis tool requires it; MySQL and hosted
server qualification do not require introducing Java into the application.

Keep Computer Use and native-client launch stopped until explicitly resumed.
Use meaningful scoped checks and required reviews; avoid full-solution builds
and full-suite runs unless explicitly requested. Keep secrets out of reports.
Update canonical status, focused evidence and remaining gates with exact tested
identities. Continue the next actionable restoration outcome after these checks;
do not mark the overall goal complete while required retail gates remain open.
```
