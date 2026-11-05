# Original-recipient incoming chat delivery

5 November 2026. Broker-admitted whisper and channel text now belongs to the
recipient present at admission and executes on that recipient's original World
queue. Replacement Players/sessions cannot inherit queued text or generate a
stale automatic response. The [portable record](coverage/incoming-chat-ownership-20261105.json)
binds the five Source/test files, isolated builds, retained failures and reviews.
F-012 remains partial.

## Real delivery boundary

`ChatWhisperTextHandler` and `ChatChannelTextHandler` capture the current
recipient's Character/realm, Player, player registry, account, session, session
epoch and EventQueue. Admission and each send check those owners, the account's
session, the session's bound Player and disconnect state. Each channel recipient
has an independent queue; replacing one recipient does not suppress another.

The World callback performs the channel's visible-sender bubble lookup and the
whisper's presence/away/busy lookup. A replacement during formatting, while the
event waits, during bubble lookup or after whisper enqueue cannot redirect the
remaining action. Automatic whispers never generate another automatic reply,
blank responses stay quiet, and a failed whisper enqueue cannot send its reply.

Existing channel ID, server-side/bubble flags, sender exclusion, realm display,
whisper/account-whisper types and automatic-response routing are preserved.
Broker membership/authorization remains owned by the existing chat service.
This does not invent new channel eligibility or durable social settings.

Routing records and text are captured before queuing. Network format conversion
creates recipient-owned values, including copies of both full-item charge/rune
lists. Later mutation of the broker envelope cannot change the queued link.

Handler completion now admits the transient delivery to the original World
queue. Format conversion still runs before that handoff and can fail broker
handling. Queued enqueue failures follow existing World EventQueue diagnostics;
this change does not promise durable delivery, a new retry protocol or exactly
once broker/network publication. No socket or two-client runtime is qualified.

## Executed failures and verification

Five baseline cases failed against published `d3fb8734` Source: whisper/channel
text emitted before the World tick, formatting-time replacement still received
text, and an epoch replacement after whisper delivery still produced a reply.
The first candidate fixes all five. The expanded19 cases also preserve normal
away/busy routing, loop suppression, independent recipients and bubble behavior.

Spec review then found nested list aliasing in the existing full-item formatter.
Two production-formatter regressions reproduced admitted charge1.5 becoming99
before the World tick, for both whisper and channel. `ToNetwork` now copies
`ChargeAmounts` and `RuneSlotItem2Ids`; outbound `ToInternal` is unchanged.

All57 affected cases pass with zero failures/skips, including21 focused cases.
Existing controls cover chat construction, format conversion, social option
handlers and World EventQueue failure/defer behavior. Final owning WorldServer
and test-only builds have zero warnings/errors. The initial isolated test-project
build reports the existing Aspire010 CLI-bundle warning; it is not a clean-zero
baseline claim. No full solution build or full suite ran.

Concurrent PvP/prerequisite Source and fixtures were changing the shared tree.
An early test build encountered local fixture naming and concurrent API drift;
neither is counted as a production negative. Qualification therefore uses an
isolated detached checkout of `d3fb8734` plus exactly these five files, with no
fixture exclusions in the qualified run. Owning/test dependency hashes match.
The root copies match the isolated candidate; unrelated work is preserved and
excluded from this task's staging.

## Standards

The initial read-only review and narrow formatter re-review report0 findings.
No actionable smell-baseline issues remain.

## Spec

The initial read-only review reports one nested-list preservation finding. Its
two executed regressions fail before the fix and pass afterward. Material
re-review confirms it resolved, with0 remaining findings. Full reports and their
separate chronology are retained with the local evidence. Final findings per
axis: Standards0, Spec0; neither has an outstanding issue.

## Remaining gates

The latest20 all-state upstream PR heads/bases/states are unchanged from the
pet-flair refresh, including608 open and609 closed/unmerged. No upstream code
was merged or PR opened. This metadata refresh adds no gameplay qualification.

Durable auto-response/ignore-strangers settings, entitlement/quota/throttle,
persistent channels and exact delivery/leave/reconnect semantics remain open.
Provider/restart/broker/socket/deployment, two-client/native and retail evidence
are still required at their recorded boundaries. Defaults15:1/17:1 and disabled
44/seven grants are unchanged. Continue the wider actionable roadmap rather than
repeating this completed broker-to-World ownership slice.
