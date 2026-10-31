# Durable presence session and admission generations

Later checkpoint7728: [Durable presence generations](FRIENDSHIP_PRESENCE_GENERATIONS.md) implements and verifies the received-event generation contract. The original plan/earlier verification below retains its date and scope; Quest and full-assessment requirements remain open.

Active implementation after7666; this is not a completion claim. Keep the full
assessment Goal, entitlement defaults and existing functionality intact. Do not
enable a partial producer/consumer protocol or claim same-character ordering
until migrations, producer wiring, consumers and recovery tests pass together.

## Authoritative order

- Allocate a strictly increasing per-account World-session epoch in the shared
  Auth database when a valid Realm hello is admitted. Use a new counter table
  under the existing Auth account row lock, with fresh whole-unit contexts and
  serializable transactions. Invalid account/email/session-key combinations do
  not allocate. Epoch gaps after uncertain commits are safe; reuse is not.
- Keep the current read-only account lookup intact. The new admission operation
  loads the same account graph and returns its epoch as runtime-only metadata.
  No password/session-key material is added to broker messages or logs.
- Each initialized Game Account/World session owns an increasing admission
  ordinal. A Player captures epoch/ordinal during initialization, so cleanup
  before successful map admission still has a stable identity. Character
  switches increase the ordinal; reconnects/realm switches/restarts get newer
  database epochs. Never use wall-clock ordering or a process-local GUID alone.
- Add AccountId, epoch, admission and UTC occurrence time to the internal
  login/logout contract as additive fields. Logout closes the same captured
  Player generation. Public client packet layouts do not change.

## Durable consumption

- Friendship stores the latest epoch/admission, source character/realm and
  closed flag on its account row. Existing rows retain default zero metadata;
  migration does not rewrite presence, history, defaults or rewards.
- Under7666 database ownership, compare epoch/admission lexicographically.
  Logout dominates login for the same generation. Reject identity collisions;
  ignore duplicate/older phases without new state or notification mutations.
  An epoch/admission-zero legacy event keeps existing behavior only before a
  versioned producer has claimed that account, so queued legacy messages cannot
  overwrite a newer versioned session.
- A newer generation supersedes the current active character. When it changes
  character/realm, also close that old character's presence atomically. This
  makes ignoring globally older events safe for per-character last-online state.
- A versioned logout can arrive before first login bootstrap. Resolve its known
  account, record a durable closed marker, and resolve/validate the character
  when available. Missing/deleted character metadata must not discard the
  account tombstone. API absence and account ownership negatives must not
  partially publish presence. State, marker and outbox settle in one unit.
- Existing preferences and normal notifications remain intact. Same-generation
  duplicates do not synthesize notifications; provider retries use fresh scoped
  models. Retain the explicit legacy compatibility boundary.

## Required verification and rollout

Add meaningful SQLite and exact-source MySQL tests for concurrent epoch claims,
invalid credentials/owners, overflow, rollback, lost acknowledgements/retry,
database identity and unchanged account/default data. Test both real handlers
across logout-before-login, delayed older same-character/different-character
events, duplicate delivery, identity collisions, character/realm switches,
superseded character closure, missing API data, retries and reopened contexts.
Verify the Game producer captures one identity per Player and monotonically
orders character switches. No secret-bearing failure log.

Generate MySQL and SQLite Auth/Friendship migrations and snapshots. Prove clean
upgrade over retained old data, default/no-backfill behavior and metadata
persistence, with other schemas untouched. Run full solution build/tests,
unchanged compatibility probes and strict content validation. Stage only owned
isolated services after migration proof; preserve all configurations/assets and
original rows. Run sustained real protocol login/world/logout, inspect durable
epochs/markers, final offline presence and natural broker drain without repair.
Native Computer Use remains stopped. Keep unmatched native/content/Quest and
the entire assessment scope open.
