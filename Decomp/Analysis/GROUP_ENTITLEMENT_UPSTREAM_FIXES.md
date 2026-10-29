# Group dirty jobs and entitlement correctness

Implemented on 2026-10-29 after protocol/network commit
`f7a1f5097a76d3aa7bab8372dc7aefdc1b176cab`.

## Group dirty jobs

Source: [upstream PR #597](https://github.com/NexusForever/NexusForever/pull/597),
head `8ca89ccb31c20a286a64ac632fca8885d812cba5`.

Both character dirty jobs now create an asynchronous DI scope per iteration.
The repository, character/group wrappers, GroupContext and OutboxMessagePublisher
come from that scope. A later iteration can therefore see a character marked
dirty again by a separate database writer instead of reusing stale tracked
values indefinitely. Outbox rows and cleared flags share SaveChangesAsync.
The existing schedules, DisallowConcurrentExecution attributes, message payloads
and cancellation check between iterations are retained.

The concrete outbox publisher is intentional: the fork's broker registration
binds IInternalMessagePublisher to Rebus, while character/group wrappers use
OutboxMessagePublisher. Resolving the interface here would bypass the transaction
boundary. The existing outbox job continues to deliver persisted messages.

Eight relational SQLite regression cases cover both jobs:

- Re-mark the first processed character dirty from a separate context after its
  first save. Both characters drain in three saves using three distinct contexts.
  The old implementation is bounded at eight saves in the regression rather
  than being allowed to spin indefinitely.
- Verify outbox payload types and one queued row per character's save, with no
  direct broker calls.
- Inject a commit failure and verify flags remain dirty, no outbox rows persist
  and no message has been sent directly.
- Verify a pre-cancelled execution leaves all characters untouched.

## Entitlement correctness and preserved defaults

Source: [upstream PR #590](https://github.com/NexusForever/NexusForever/pull/590),
head `163666c34b0a8efcd92fa062efd265751f4e77f1`.

Only the correctness changes are ported:

- Bit 0x2 is Hidden rather than Disabled. It no longer blocks explicit grants
  or zeros entitlement-backed reward modifiers.
- Apply the same correction to the fork's account-item claim and direct
  storefront grant validators. Account/character scope, static-data checks,
  amount limits, purchase charges and rollback paths remain in force.
- Newly created character entitlements are marked for INSERT. Loaded rows are
  updated when their amount changes and are not inserted again.
- Invalid initial amounts report the supplied entry's ID and limit instead of
  dereferencing Entry before initialization.

AccountDefaultEntitlements is unchanged: EconomyParticipation (15) and
FullSocialParticipation (17) retain their existing baseline policy. No seven-item
automatic character entitlement grant from upstream #590 is added. The character
manager constructor, player login and baseline migrations are unchanged.

Regression coverage includes normal and hidden account/character grants,
scope and amount guards, account and character reward modifiers, a real SQLite
insert/load/update round trip, constructor error reporting, and existing
account-item/storefront flows exercised with both normal and hidden flags.
The existing default tests continue to assert exactly the two baseline account
grants; empty character managers still contain no automatic grants.

## Verification

The first 22-case targeted slice reproduced 11 failures before production edits
and passed 22/22 after the fixes. The extended relevant slice passed 71/71.
There are 22 additional cases in the full suite: eight group-job cases, six
entitlement cases, three reward-modifier cases, three account-item cases and two
storefront cases.

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo
git diff --check
```

On .NET SDK 10.0.300 the full build passed with zero errors and the existing
114 warnings. All 5,832 tests passed with zero failures and zero skips. Local
logs and TRX files are in
`I:\GIT\NexusForever-assessment-report\group-entitlements`.

These regressions exercise separate SQLite database contexts and the real
group/outbox code. Live MySQL/broker stress and client gameplay smoke were not
run. No database schema or migration change is required.
