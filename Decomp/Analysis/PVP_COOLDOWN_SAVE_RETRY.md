# PvP cooldown save retry

6 November 2026, parent `7bec78cb`. The existing public PvP toggle-off request
sets a durable pending expiry; toggling on cancels it. Player.Save previously
cleared its PvP dirty bit during enrollment. An unconfirmed save therefore lost
both newly requested expiries and cancellation intent on a fresh-context retry.

The pending expiry now remains dirty until its tracked write has flushed and
the Character transaction settles successfully. An unrelated or suppressed flush,
rollback, disposal, unflushed commit and savepoint rollback cannot acknowledge
it. The original Character and a per-Player version protect later requests and
request/cancel transitions returning to the same null value. Busy map admission
conservatively skips enrollment or acknowledgement without blocking persistence;
a subsequent save retains the pending value.

Dirty-mask updates now use atomic bit operations on the existing int-backed enum.
This prevents an acknowledgement from overwriting another owner's bit update
without adding locks to the other scalar setters. Location retains its own
settlement/version checks. Other scalar acknowledgement policies are unchanged;
this is not complete retry recovery for every Player scalar field.

The existing cooldown constant, immediate packets, timer/restore behavior,
forced/enabled flags, duel-only combat boundary and entitlements are unchanged.
No packet/model/schema or retail-policy expansion was made.

## Verification and limits

- Baseline:11 failures/one pass across12 new cases against the unchanged parent
  Game. Both parsed request and cancellation lose their SQL intent after failed
  enrollment/settlement. The later-request control passes and is retained.
- Final production candidate:183 scoped SQLite cases pass in42seconds, including
  PvP, location/taxi, normal Player-save snapshots, Character completion/validation
  and map admission. Prototype117/119 passes and the corrected misplaced-using
  compile failure remain separate local records.
- Final fixture uses one bound World session, account and event queue for the
  real Player and parsed public handler. All24 PvP cases pass, including16 new
  persistence cases. The default SQLite assertions remain; the optional provider
  constructor only supplies the existing fixture's explicit provider options.
- All16 same persistence cases pass on configured MySQL8.0.46, with the repository
  retry strategy and exact49 migrations per fresh schema. An independent temporary
  Character row measures stored datetime precision; the Player row must match it.
  Production time conversion/precision remains unchanged.
- The earlier16-case MySQL pass used a separate handler-session collaborator;
  the final bound-session run supersedes that fixture boundary. Their totals are
  not added. The first attempt failed only because its generated schema made
  MySQL's migration-lock identifier exceed64 characters. Its schema was removed
  and subsequent attempts use shorter leased names.
- All33 newly created schemas across retained attempts are independently absent.
  Original MySQL processes2648/6016 remain running. No original schema, service,
  broker, client, authoring table or entitlement was changed by this pass.
- Owning Game build passes with zero warnings/errors. Normal Game.Tests graph
  builds pass; the existing ASPIRE010 warning and local adapter CS8604 warning
  remain recorded separately. Owning/test/adapter Game DLLs match; final test and
  adapter test DLLs also match. No full solution or full suite ran.

Primary-agent Spec review checks flush/terminal acknowledgement, original
Character, later/null transitions, shared-mask updates, nonblocking map admission,
provider precision and public-session binding. Standards review checks the six
owned Source files, existing transaction callbacks and fixture provenance. Both
have no remaining actionable finding; no independent reviewer is claimed.

[Portable evidence](coverage/pvp-cooldown-save-retry-20261106.json) binds source,
DLLs, TRX, exact migration identities, provider outcomes and cleanup. Raw evidence
and the small provider adapter remain ignored at `artifacts/pvp-cooldown-save/`;
transfer those separately without publishing private settings. The adapter invokes
the tracked persistence cases through their explicit provider-options constructor.

This qualifies Character save/retry and fresh Player cooldown initialization with
prepared account/session collaborators. Full deferred Auth/Character lifecycle,
physical process/database loss, hosted/socket reconnect, native UI and retail
qualification remain separate. Earlier hosted results keep their own frozen Game
DLL boundary. F-015 and the overall Goal remain unfinished; native-client launch
and Computer Use stay stopped. Continue the broader gameplay/progression queue.
