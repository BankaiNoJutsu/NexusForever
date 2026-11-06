# Friendship account commands and presence ownership

6 November 2026, parent `ec278294`. Public presence, stranger blocking, public
status and nickname commands now carry the original Character/realm, account
session epoch and presence admission to Friendship. The existing persisted
presence owner gates their mutation and outbox under the account transaction.
No client wire, database schema or entitlement changes.

## Reproduced failure and delivered behavior

Four baseline cases changed settings through unversioned commands after a
modern owner had been recorded. The baseline fixture retained the model's
closed-generation default: these are closed-owner overwrite failures, not
proof of an active retail session. Its four positive controls passed against
the old handlers. The new optional command metadata was present for this
baseline; the four setting handlers still used the parent behavior.

World now admits these four parsed requests only from its bound Player/account,
with a nonzero captured presence epoch/admission matching the Account epoch.
The Character and realm come from that Player. The metadata is copied into the
internal command and survives normal serialization. Presence keeps its existing
immediate local intent update; a broker publication is not a database receipt.

Friendship uses the existing presence transaction and account-row ownership:

- Reload the stored Account under transaction ownership before deciding, even
  when EF or the scoped account cache already loaded an older row.
- Apply a modern command only to its exact recorded open epoch, admission,
  Character and realm. Ignore older, closed and foreign commands without
  changing the setting or creating a result/outbox message.
- A command ahead of the recorded login throws before mutation. Normal message
  retry can admit it after its actual `PlayerLoggedIn` event. It does not create
  or advance a presence owner itself. Missing modern accounts have the same
  prerequisite behavior.
- Reject incomplete modern metadata. Old commands keep their existing behavior
  only on unversioned legacy accounts; they cannot overwrite a modern owner.
- Stage the setting and its existing readback/result outbox together. SQL
  refusal rolls both back. Whole-unit provider retry clears the EF tracker and
  the scoped Account/Character caches before re-evaluating ownership.
- Modern validation errors target the admitted owner, rather than a separate
  legacy target field. Existing text filters and setting semantics are retained.

Deploy updated World and Friendship with the shared internal-message assembly
together. An older World emits unversioned settings, which updated Friendship
intentionally ignores once that account has a modern presence owner.

This is a guard against the **latest recorded Friendship presence generation**.
It is not a new distributed Auth lock: a replacement not yet recorded by
Friendship cannot be inferred. Commands within one admission have no new
revision/nonce, and already committed readbacks can still be reordered.

## Executed qualification

All 74 new cases pass within 188 related Source/SQLite cases. Twenty new cases
use parsed requests and a real Player, serialize the actual published command,
then enter the real Friendship handler and disposable SQL/outbox path, or reject
an unbound/uninitialized/rebound source before publishing. The remaining cases
cover all four commands across legacy/current ownership, older epochs and
admissions, foreign Character/realm, closed presence, incomplete metadata,
cached preimages, actual login-before-retry and validation-error recipients.

For each command, a transaction interceptor observes the flushed outbox and
refuses commit. Independent SQL reads retain the original setting and no outbox;
a fresh handler retry succeeds. A separate execution-strategy transient after
flush retries the whole unit with a different Account model instance and commits
one outbox. These are in-process SQLite fault controls, not configured MySQL,
ambiguous post-commit or physical process-loss qualification.

Existing controls include friendship invitation/readback/flag persistence,
presence login/logout generation and transaction recovery, producer serialization,
and incoming chat ownership. Intermediate compilation and fixture failures are
retained separately: the first helper needed an Account alias; later fixtures
omitted the actor queue and the model's explicit open-generation flag. Their
correction did not relax production guards. The final related run passes in
9 seconds. Owning Game, Friendship and World builds are recorded with the
[portable source/DLL/result record](coverage/friendship-account-command-ownership-20261106.json).
The test graph retains its existing ASPIRE010 warning; no full solution or full
suite ran. Raw outputs remain under ignored `artifacts/friendship-command-owner/`.

Both final review axes are performed by the primary agent. The specification
review corrected modern validation-error addressing and added two cases; final
specification and repository reviews have no remaining findings. Refreshed
upstream #608/#609 heads, bases and states are unchanged; reviewed unchanged
diffs are reused without an upstream merge or PR.

## Auto-response investigation and remaining work

This pass also inspected the retained same-digest build16042 setters at
`1405de2e0`, `1405df560` and `14070e4a0`. They keep away/busy strings, supply
localized defaults when empty, and send both strings with `03B7` when ready,
retaining a pending-send flag otherwise. The public status callback can set
one response alongside presence. These bodies do not establish a server-side
durable storage owner or a restore/readback protocol.

The existing NCsoft-authored
[FriendsList](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/FriendsList/FriendsList.lua),
[ChatLog](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/ChatLog/ChatLog.lua),
[OptionsInterface](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/OptionsInterface/OptionsInterface.lua)
and [protected Options](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/ProtectedAddons/Options/Options.lua)
mirror files supplied no `SetAutoResponseMessages` restore call in this bounded
inspection. This older authored corpus is corroboration, not final-build Lua
identity or proof that no other restore path exists. Exact web searches returned
no additional result. Auto-response storage stays unchanged pending the specific
native restore/initialization or accepted reconnect witness; no guessed Account
or Character column is added.

Live broker redelivery/order, current configured providers/copied upgrades,
deployment transitions, physical restart, two-client/socket/native/retail,
within-admission command ordering and stale committed readback revision remain
open. Full friendship/F-012 and the unified Goal are unfinished. Continue the
broader integrated gameplay and assessment scope; client launch/CUA stay stopped.
