# Friendship presence transaction ownership and refresh

Later checkpoint7728: [Durable presence generations](FRIENDSHIP_PRESENCE_GENERATIONS.md) implements and verifies the received-event generation contract. The original plan/earlier verification below retains its date and scope; Quest and full-assessment requirements remain open.

30 October2026. The concurrent stale-tracker write defect is implemented and verified. **Durable session/event generation remains open**: this change does not establish order for an old same-character login/logout delivered after a newer session, or close every first-bootstrap/reordered-message case. The full assessment Goal remains active.

## Defect and resulting behavior

The7648 runtime observed synthetic Account26 active on Character73 after complete World cleanup. Handler logs show logout clearing identity while a login transaction is finishing. Two deterministic tests reproduce the lost-write mechanism against the old handlers: a context caches the target presence, another writer changes SQL to the opposite state, and assigning the cached original value does not mark the actual required columns modified. Both directions failed before the fix; their original TRX is retained. This reproduces a defect consistent with the runtime observation, without claiming that interleaved historical logs prove its exact transaction history.

`FriendshipPresenceTransaction` owns an explicit transaction for both real handlers. MySQL uses the existing account row FOR UPDATE; SQLite uses a serializable writer transaction. The handler refreshes the tracked root under ownership and refreshes the character before mutations. Model identity resolution alone cannot return a stale EF baseline. Presence, active identity, last-online values and their real outbox notifications settle together. A different old character/realm logout updates that old character but preserves the current account identity and presence. Login verifies character/account ownership.

Whole-unit provider retries discard rolled-back tracking and scoped Account/Character wrapper caches before reading again. The initial scope rejects an existing transaction or pending writes so this reset cannot discard unrelated edits. Missing dependencies return through rollback, preserving the prior bootstrap boundary. Preferences, nickname, status, friendship notifications and existing active-identity presence rules are preserved. No schema, migration, packet, entitlement or default changes were made.

## Verification

Evidence: `I:/GIT/NexusForever-assessment-report/remaining-goal/friendship-presence/final-review`.

- Full `Source/NexusForever.slnx` build: **0 warnings/errors**; full suite **7,666/7,666**, zero skipped. Full TRX SHA256: `FBB9A29316C512B57E8A8E53FE50EBA24D261C32CB82813C64137BCC82405322`.
- **18 new tests**, including two cached-opposite-state failures retained before the fix; **42 focused cases** include existing Friendship bootstrap/Query behavior. Actual handlers, contexts, models and outbox are exercised. Cases cover overlapping independent handlers, stale roots/leaves, state/outbox rollback, injected whole-unit retries before flush/before commit, different character/realm logout, foreign/missing dependencies and unrelated pending writes.
- **18 exact-source MySQL cases**, no exclusions, pass in fresh retained `nexus_assessment_friendship_presence_30a1`. Per-case Account/Character/outbox snapshots are hashed before cleanup of only the dedicated synthetic graph. Overlap cases exercise real database ownership; retry faults are injected, not a claim of an observed MySQL deadlock. Current-model EnsureCreated is not migration proof.
- Verification binds **169 DLLs/5,743 Source files**, six changed Source paths and the copied exact test. **31 unchanged upstream compatibility probes** pass. Relevant Game/Character/Auth database/World binaries are byte-identical to7648; its281-case Character reward/Quest provider proof retains its historical scope and is not counted as18 new Friendship cases. The initial freeze's absent AuthServer test-dependency lookup was corrected; immutable snapshots were revalidated without overwrite.
- Frozen FriendshipPID8220 passes **221 complete world/bootstrap/ACK/disconnect/cleanup cycles**, with221 composed reward reloads. The70-second mixed run passes219/219, plus Eastern/existing-account single cycles. The exact appended World log range is bound to its prior prefix; World/Auth stayed running. Only Friendship restarted, all8 other process identities and all9 service configurations were preserved. All6 queues drain naturally; final12 synthetic presences are offline/null-active **without SQL repair or a recovery relog**. Original data, exact15:1/17:1 defaults/no44 and empty new Character reward tables/null legacy attempt markers pass. No new native UI or live Challenge earning is claimed.
- Strict content validation remains31 CSVs/167,484 rows, all not_retail_complete. Original `I:/GIT/NexusForever` remains clean, branch/head unchanged, no commit/push. Computer Use stays stopped.

## Remaining work

Add an authoritative durable session/admission generation contract across producers, handlers and broker delivery before claiming general presence ordering. Cover logout during first bootstrap, delayed older same-character login/logout, duplicate/redelivered events, character/realm switches, retries and restart. A row lock orders overlapping SQL writers; it cannot identify an older event delivered later. Preserve existing friendship preferences/outbox behavior while implementing that contract.

Quest actor participant ownership/adoption, live Challenge earning and real network earning/choose/claim/relog/reset remain open, together with the wider assessment/matrix requirements. The earlier runtime7648 rejected reconnect and stale-presence observation remain retained under their own scope.
