# Query and friendship cache bootstrap concurrency

Implemented during the 2026-10-29 remaining-assessment goal in
`I:\GIT\NexusForever-assessment`. Real isolated MySQL/broker smoke exposed two
first-use cache races that earlier unit-only checks had not exercised:

- CharacterServer: duplicate `character` primary key `44-1` while independent
  PlayerLoggedIn/WorldZoneUpdated/stat/logout scopes fetched the same missing
  API character and each queued an INSERT.
- FriendshipServer: duplicate `account` primary key `7` during concurrent first
  logins sharing an account. Independent scopes each fetched the missing API
  account before the initial save.

The original traces are preserved in the goal report's
`runtime-runs/pre-final/NexusForever.Server.Character.stdout.log` (initial failure
at line338) and `runtime-runs/pre-final/NexusForever.Server.Friendship.stdout.log`
(failure at line3010). Those runs
use the isolated `nexus_assessment_20261029_*` databases, not a production data
repair. No database rows were manually deleted or overwritten to conceal the
problem.

## Atomic bootstrap and payload ownership

Query CharacterRepository now seeds a missing row atomically. SQLite names the
composite conflict target `(characterId,realmId)` explicitly; MySQL uses a no-op
duplicate-key update. The winning row is fetched for tracking, so a stale API
snapshot never replaces another message's level, zone, guild or presence state.
For MySQL callers with an existing transaction, a locking current read avoids
returning an older repeatable-read snapshot after a competing insert commits.

CharacterManager's update path fetches remote data before opening its own
transaction, then performs seed, message-owned column update and SaveChanges in
one unit. An existing caller transaction retains commit/rollback ownership.
The complete owned unit runs inside the provider's execution strategy. A first
MySQL warm rerun exposed that the configured retrying strategy rejects explicit
transactions created outside it; this was corrected without disabling retries.
Configured-strategy regressions now cover transaction ordering, whole-unit retry
after a transient post-seed failure and caller-owned rollback.
The five player handlers use this path for LastOnline, WorldZoneId, Level and
GuildName. The specified field is explicitly marked modified even if its new
value equals the context's original snapshot; unrelated fields are not marked
modified. This preserves authoritative no-op payloads without overwriting
concurrent changes to other columns. Public remote lookup returns the winning
cache row without leaving another pending INSERT in the context.

Messages do not carry sequence versions, so this does not claim to solve
out-of-order delivery of different values for the same field. It fixes duplicate
creation and unrelated-column lost updates; database commit order still governs
same-field writes.

Friendship AccountRepository uses the same atomic seed principle for account-ID
and email bootstrap. The winning account retains preferences, nickname, status,
presence, active-character fields and existing relationships. A MySQL collision
on a different account's unique email/nickname cannot be treated as a successful
same-account bootstrap: the requested account ID must exist after insertion, or
the method throws. Other database errors are not caught or suppressed.

The existing Friendship login and email-invite transactions still encompass
bootstrap, friendship mutations and outbox writes. MySQL uses ReadCommitted for
these two remote-bootstrap callers so relationship reads after a competing
first-create transaction see committed state. SQLite retains Serializable.
Existing publisher/outbox calls and commit boundaries are unchanged.

No packet fields, message payloads, database models/migrations, account ownership,
entitlement defaults or storefront grants change in this slice.

## Regression evidence

Twenty-four new relational SQLite cases exercise actual repositories/managers
with independent contexts and an HTTP stub at the existing API client boundary:

- Concurrent API misses create one row and retain both zone and level messages.
- A late API response cannot reset another context's winning data/preferences.
- Repeated login payloads still clear LastOnline without reverting newer level
  updates, and presence/guild handler wiring preserves other fields.
- Failed payload save rolls back the bootstrap; an outer transaction owns its
  final commit/rollback.
- Character IDs are scoped by realm; missing APIs and ignored stat messages do
  not create rows.
- Public lookups retain scope caches without pending duplicate inserts.
- Friendship bootstrap and outbox rows commit/rollback together.
- Existing friend relationships survive duplicate bootstrap, and an unrelated
  unique-email or trigger failure is not suppressed.

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter 'FullyQualifiedName~CharacterQueryBootstrapTests|FullyQualifiedName~FriendshipAccountBootstrapTests'
```

The focused run passes **24/24**, with zero failures/skips and no new analyzer
warnings (`remaining-goal/bootstrap-retry-focused.log`). Its first friendship
relationship fixture exposed a missing FriendAccount join row; that fixture was
corrected to populate the actual schema, without weakening production constraints.

The subsequent integrated checkpoint passed a full build with zero warnings or
errors and 6,249 tests. The isolated real MySQL/broker run then passed one warm
login and all 36 samples for six fresh owned users. Query characters, friendship
accounts and friendship characters each grew from zero to six for those users;
online counts returned to zero after logout. No new duplicate inserts, execution
strategy failures or read-model errors appeared, queues drained, and the four
historical dead letters remained unchanged. Evidence is in the assessment report's
`remaining-goal/load-cold-checkpoint6249` and `load-final-after-state.tsv`.
This exercises the MySQL atomic bootstrap, ReadCommitted friendship transactions
and provider execution-strategy wrapper; it does not establish a graphical-client
or retail-completeness claim.
