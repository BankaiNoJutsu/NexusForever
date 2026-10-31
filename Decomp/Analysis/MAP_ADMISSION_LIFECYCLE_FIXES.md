# Map admission and entered-world lifecycle

Status: implementation, independent review, full solution verification and corrected isolated-runtime replay complete at checkpoint 6717. The final solution build has zero warnings/errors, all 6,717 tests pass, and the owned World runtime passes 26/26 lifecycle cycles.

## Reproduced failure

The frozen 6541 World log (`NexusForever.WorldServer_20261029_20444.log`, retained under the external assessment report) records the account 15 / character 62 Eastern entry twice acknowledging `ClientEnteredWorld` before assignment to a map. `IsLoading` started true on the newly created Player. Character selection queued MapManager admission, while the old handler accepted that loading flag as readiness.

UTC timeline on 2026-10-29:

- 17:39:35.6010: first player-manager insertion; 17:39:35.9287: entered-world request; 17:39:36.0221: disconnect with playerGuid/world/zone all zero; 17:39:36.3865: cleanup completed.
- 17:39:36.7252: another insertion for the same character; 17:39:36.9006: entered-world request; 17:39:37.0850: another disconnect with zero map identity; 17:39:37.4128: final cleanup completed.
- 17:39:37.8980 and 17:39:38.2102: post-add initial packets and login/world-updated publication continued for those old objects. BaseMap admitted entity GUIDs 2 and 2655 to world 51 after final cleanup, with no subsequent account 15 login.

Parent's read-only corroboration found no established World TCP connection but Friendship account 15 still online (presence 0, active character 62, realm 1), while 16/17 were offline (3/null/null). Evidence: `source-checkpoint6660/AuthMigration/synthetic-session-state.json` and `inspect-sessions.log`. The original databases and credentials were not changed.

The two existing-API negative controls both fail against unchanged production in `map-admission-negative-control.log` and `map-admission-negative-control/map-admission-negative-control.trx`: an entered-world request invoked the player with no assigned map; a finished queued player reached CreateMap. These were actual wrong-call failures, not fixture/build errors.

## Implemented ownership

An optional IPlayerMapAdmission capability preserves existing IPlayer and custom implementations. The real Player owns a monotonically advancing admission generation and one synchronization gate. Queueing starts a loading transition; cleanup/disposal invalidate that object permanently. BaseMap's add/pending actions and both MapManager and InstancedMap queues retain the captured generation. Stale actions cannot activate a grid, insert a player, create an instance from already canceled work, retry an unloading instance, or consume an instance capacity reservation. Duplicate direct player adds are discarded after the first assigns a map; legitimate pending transfers continue waiting for removal. Valid queued reservations and existing map-lock/instance limits remain intact.

The handler ignores requests before Map and GUID assignment. Once assigned, Player coalesces a valid request received while bootstrap is running. The outer BaseMap virtual AddEntity completion releases the request only after Player initialization, login callbacks, public events, map scripts and instance/housing post-base callbacks return successfully. Duplicate entry requests have no second completion. Post-base instance continuations recheck the captured generation to stop synchronous script cancellation from continuing registration or housing packets.

Cleanup/disposal share the admission gate with graph insertion and bootstrap; CanCleanup also respects an active bootstrap marker. A failed callback releases the marker without readiness. Teleport failure restores the previous map's loading state, and completed entry consumes its old pending teleport before recovery callbacks run, preserving a newly queued recovery teleport instead of clearing it at the old tail.

No scheduling mode change, dropped spawn/script population, timeout inflation, protocol-default change or entitlement-default change is part of this fix. The current hosted-service call path invokes network then map updates inline; the unused SynchronousUpdate configuration flag is not evidence of an active parallel map scheduler. Synchronization additionally covers supported reentrant cancellation and possible overlapping callers.

## Verification

The initial combined gate (`map-harness-entitlement-first.log`, `map-harness-first-tests/map-harness-entitlement-first.trx`) passed all 27 initial server cases, all 8 harness cases and selected existing controls. Overall 315/317 passed; the two failures reproduced a separate earned-entitlement rollback defect, subsequently corrected in the integrated checkpoint. Two xUnit blocking-task warnings in the first compile were corrected with bounded async waits.

New server regression classes: WorldMapAdmissionLifecycleTests, PlayerMapAdmissionStateTests, MapAdmissionQueueTests (32 final cases, all passing). They cover pre-admission rejection, assigned-map deferral/coalescing, cancellation, superseding/recovery generations, failure and duplicate completion, old-object/reconnect independence, real handler-to-Player deferral, map/instance retries, instance reservation capacity, grid-activation cancellation, outer callback ordering/failure, cancellation serialization, three real-Player script cancellation boundaries, duplicate direct-add rejection, and a real Player entered-world callback that queues a replacement teleport and preserves its pending state/loading flag.

The final focused gate passed 62/62: 32 server lifecycle cases, 8 harness bootstrap cases, 17 earned-entitlement cases and 5 legacy entry controls. The broad focused gate passed 376/376. Three older entry fixtures were updated to supply a legitimately assigned Map/GUID and completed admission; their assertions were preserved, and independent review approved the fixture correction. Evidence: `map-entitlement-final-focused.log`, `map-harness-entitlement-focused.log` and their TRX files under `map-harness-entitlement-tests`.

The full solution build completed with zero warnings and zero errors (`map-entitlement-integrated-build.log`), and the full suite passed 6,717/6,717 (`map-entitlement-integrated-tests.log`, `map-entitlement-integrated-tests/map-entitlement-integrated-suite.trx`). The corrected LoadTest waits for actual bootstrap packets under bounded I/O deadlines.

## Corrected isolated live replay

The frozen, verified 6717 World output ran as owned PID 44372. The corrected harness passed 26/26 cycles: one cold Eastern login for fresh 9401, 24 mixed Eastern/NPE cycles across fresh 9401–9403, and one normal repair login/logout for old synthetic 9301. This explicitly covers the larger Eastern bootstrap that exposed the original blind spot.

`runtime-checkpoint6717/lifecycle-log-audit.json` reports success with 26 requests, replies and assigned nonzero-map disconnects, all 26 admission/bootstrap/cleanup cycles complete, zero unfinished admissions, zero late or duplicate bootstraps and zero errors. The retained World log is `runtime-checkpoint6717/NexusForever.WorldServer_20261029_44372.log`; parsed events are in `lifecycle-events.json`.

All six synthetic accounts 15–20 finish offline with presence 3 and null active character/realm. Old account 15 changed from stale 0/62/1 to 3/null/null through that ordinary repair login/logout; no SQL presence reset was used. Broker queues finish at zero. Read-only final audits preserve all 77 original tables, exact full-row hashes for the 43 copied legacy characters and all 13 old owned entitlement rows. Evidence: `runtime-checkpoint6717/runtime-6717-final-state.json`, `post-gate-audit.json`, `broker-after.log` and the parent summary `RUNTIME_CHECKPOINT6717.md`.

This closes the corrected synthetic protocol/bootstrap/cleanup gate for checkpoint 6717. The user's earlier confirmation that the native world loaded and responded normally remains a separate trusted visual/responsiveness result from the earlier native session; no fresh native visual gameplay, authentication UI automation or new retail-parity claim is inferred from this synthetic replay.
