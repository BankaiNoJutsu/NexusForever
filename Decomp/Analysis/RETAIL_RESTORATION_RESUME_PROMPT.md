# Resume retail restoration on the asset-owning machine

Paste the following into a chat on the machine with the runtime assets.

```text
Resume the NexusForever retail restoration for WildStar build16042. Start with
provider packet timestamps, then hosted World/Auth restart qualification.

Locate the actual checkout and verified runtime assets; do not assume historical
drive letters, paths or process IDs. Read AGENTS.md, Tools/Setup/README.md,
Decomp/Analysis/RETAIL_RESTORATION_GOAL_PROMPT.md and
Decomp/Analysis/PROVIDER_PACKET_HOSTED_RESTART_HANDOFF.md. Preserve unrelated work
and resume the existing restoration goal without creating a duplicate.

Current checkpoint:
- The previous achievement handoff milestone passed51 MySQL cases,344 scoped
  SQLite cases, six forced producer-loss windows plus a normal control, and
  pending/delivered recovery across a normal MySQL restart. Reuse retained
  evidence where applicable; these are headless checks, not hosted qualification.
- All private qualification MySQL generations were stopped. No hosted World/Auth
  restart was qualified. No production packet-timestamp change was made.
- Six draft timestamp tests failed because the fixture captured no Guild update
  packet, before timestamp comparison. This is not a timestamp defect witness.
  The portable draft is retained outside active Source as
  Decomp/Analysis/continuation/GuildAchievementPacketTimestampTests.draft.cs.txt.
- Microsoft documents that DateTime.ToFileTimeUtc treats an unspecified Kind as
  UTC. Do not assume MySqlConnector's default unspecified Kind causes an offset.

First finish provider packet timestamp qualification. Inspect actual Guild
broadcast recipient registration and committed publication draining, repair the
fixture and resolve its analyzer warnings. Exercise configured MySQL-loaded rows
through committed updates, fresh-owner initial packets, legacy row Build and
Character completion publication. Serialize and read the actual wire fields.
Cover null completion, winter, summer, both clock-change boundaries and
fractional seconds. Compare with stored SQL dates, accounting for provider
precision. Preserve progress, credited masks, actor ownership and precommit
publication exclusion. Keep production unchanged if these checks agree; fix
behavior only after reproducing a real defect.

Then qualify hosted World/Auth restart. Verify the build16042 table/map cache,
private configuration, database routes, broker and current owned service
generations. Read Tools/NexusForever.LoadTest/README.md before protocol runs.
Prepare a frozen candidate using incremental World/Auth project builds. Restart
only validated owned World/Auth generations through the documented mechanism;
preserve other services, original data, entitlements and broker queues. Require
actual hosted readiness and complete authentication/world/bootstrap/disconnect/
cleanup cycles before and after restart. Exercise pending achievement feedback
and duplicate recovery where supported, retaining exact row, receipt, packet
and lifecycle witnesses. Generic login success does not prove achievement earning.

Use meaningful scoped checks and required reviews. Update canonical status,
evidence and remaining-work records with exact source/DLL identities and cleanup
proofs. Keep hosted startup, socket login, achievement recovery, native behavior
and retail parity separate. Computer Use and native-client launch remain stopped.
Do not fabricate assets or weaken validation. Avoid full-solution builds and full
test-suite runs unless explicitly requested. The overall restoration remains
unfinished; continue to the next actionable outcome after these milestones.
```
