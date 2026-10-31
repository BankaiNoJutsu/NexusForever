# Load-test seed account defaults

The 6,918 runtime checkpoint completed 20/20 protocol sessions with 20 matched entry requests/replies and assigned disconnects, no errors, unfinished admissions or late/duplicate bootstraps. Its scenarios were one cold Eastern entry, 18 mixed-world sessions and one existing-account entry. The separate post-default audit then failed: freshly seeded loadtest9501/account21 had **no entitlement rows** (`remaining-goal/runtime-checkpoint6918/RuntimeAudit/observed-defaults-21.json`). Protocol success therefore did not establish default correctness. The failure remains recorded in `initial-default-failure.json`; no SQL repair was applied to make that account pass.

`SeedCommand.Run` directly constructed AccountModel and bypassed the canonical AccountDefaultEntitlements.EnsureBaseline call used by normal AuthDatabase.CreateAccountAsync. It now calls internal `CreateNewAccountModel(email,password)` only inside its existing `account == null` branch. The helper preserves real SRP salt/verifier generation, Email and UTC CreateTime, then adds exact canonical15=1/17=1. It creates no44,7, inventory/pending items or Loyalty grants. Existing-account behavior, custom entitlements, credentials, role insertion and profile/character generation are unchanged.

Five dedicated LoadHarnessSeedAccountTests cover exact defaults/no unearned rewards, real SRP regeneration with16-byte hex salt and UTC timestamps across three credential cases, and independent baseline row ownership. The focused selection passed 16/16; the final full build passed with zero warnings/errors and all 6,923 tests passed, including these 5. Evidence: `remaining-goal/seed-defaults-focused.log`, `seed-defaults-full-build.log`, `seed-defaults-tests/seed-defaults-full-suite.trx`.

Read-only live fixture checks after running the corrected seeder confirm:

- Existing9501 negative control/account21 still has no entitlement rows. `runtime-checkpoint6923/RuntimeAudit/existing-after-result.json` reports exact preservation of 70 captured rows across 10 tables, including credential/role/character/reward state. This intentionally retains the earlier failure, proving the seeder did not repair or overwrite that existing account.
- Fresh9601/9602/9603 map to accounts24/25/26 and characters71/72/73. Before world login, `RuntimeAudit/seed-defaults-result.json` records exactly`15:1`,`17:1` for every account, with no44. Fresh9601's owned synthetic character was placed at the prior native-observed Eastern position;9602/9603 retain NPE placement. Account seeding creates no inventory/pending items or Loyalty grants; the character profile path is unchanged.

The final6,923 live gate passes23/23 sessions: one cold Eastern,21 mixed-world
and one existing-account entry. `runtime-checkpoint6923/lifecycle-log-audit.json`
matches23 actual bootstrap/assigned-disconnect/cleanup cycles with no late adds
or errors. The post audit confirms all three fresh accounts retain exact15/17,
zero wallet/earned/claimed/pending rewards and no44; all12 synthetic accounts
are offline/null-active. Original77 tables,43 copied legacy characters and13
old entitlement rows remain unchanged. Runtime World/PID39024 is the only
restarted service; all six broker queues drain. The earlier6,918 default failure
remains separate historical evidence.
