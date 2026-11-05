# Ordinary guild creation fee settlement

Later Community result: [public credit creation finance](COMMUNITY_CREATION_FINANCE.md)
now couples its separate guild/root/plot graph with Character credits through a
dedicated Community receipt.15 new Community/22 ordinary finance and278 affected
cases pass. This supersedes the pending Community credit next step below;
durable Auth Service Token creation and other qualification gates remain open.

5 November2026. Public ordinary Guild creation is implemented and verified in
Source/disposable SQLite. The [portable milestone record](coverage/guild-creation-finance-20261105.json)
binds19 Source files, seven root/test DLLs, retained negative regressions,
22 focused/202 affected passes, clean builds and final Source reviews.
The acceptance boundary is the real public ordinary Guild creation route:
accepted credits, complete guild/data/rank/leader-member graph and chosen
Character affiliation settle in one recoverable Character transaction. Success
and join/roster notices wait for committed persistence and original-actor
publication. Community credit/Service Token creation remains separate work.

## Required behavior and implementation

- Capture the accepted GameFormula764 fee once. Public real-Player ordinary Guild
  registration uses the coordinator; free direct/admin registration, legacy
  adapters and other guild types retain their existing behavior.
- Reserve the unique name and retain a private draft before touching the fee.
  The draft is absent from the public registry and periodic guild saves. Its
  leaf/standalone saves and mutations cannot bypass the fee owner.
- Freeze the original Character save through the existing fee actor, including
  wallet, quests/rewards, inventory and affiliation. The reservation blocks
  spending while the visible balance remains unadopted until commit.
- Enroll the complete draft with the fee and original Character graph in one
  serializable transaction. A narrow creation receipt authorizes only the exact
  accepted guild/data/three ranks/leader member rows. Callback authorization must
  not permit foreign guild rows.
- Reconcile commit uncertainty with the full captured receipt. Proven rollback
  retains the original snapshot for retry; unknown outcomes cannot replay or
  release ownership. Use the existing receipt table, without DDL/backfill.
- Adopt local membership after primary confirmation, then complete the ordinary
  save for committed join achievements and deferred currency intent. Keep the
  draft private and fenced until that followup is confirmed; only then register
  it publicly and release its mutation fence.
  Real Character join progress is captured for the ordinary save. Guild
  forwarding effects drain before the next ordinary snapshot under a narrow
  original-manager/current-thread achievement permission. Structural edits and
  independent saves stay fenced. Guild completion feedback therefore enters
  that Character snapshot; later forwarded effects force another ordinary
  phase. Rejected effects retain their cursor without re-crediting the original
  Character join or charging again. Guild achievement rows retain their
  existing separate persistence/publication owner.
  Preserve an existing affiliation and later accepted selections. Do not rerun
  creation gameplay merely to retry a rejected network/broker publication.
- Publish through a retained cursor, construct fresh rejected packets, protect
  the original session/queue/account epoch and prevent reentrant world updates
  from starting another attempt before the first task is retained.

This is ordinary server transaction work; it needs no new client semantic claim
or REA provider call. The existing frozen fee actor retains its historical
housing names. Each concrete operation supplies separate receipt authorization.

## Current evidence

Local evidence is under `artifacts/continuation-20261105/guild-creation-finance/`.
On base84481977, the public-flow baseline failed three assertions: early success,
an independently persisted unpaid guild and an affordability-based provisional
balance assertion. The latter conflated reservation blocking with visible
adoption. Its corrected regression checks actual visible balance and blocking
separately; the original failed run remains retained, not reclassified as proof
of the corrected assertion.

The first candidate passed10 of11 cases. The remaining affordability oracle was
corrected as above. A separate reentrant-Auth/world-update regression failed on
that candidate; the coordinator now guards its advance transition. The executed
failure remains retained. Initial focused14/affected153 cases then passed. Spec
review found two missing boundaries: direct member/rank removal could bypass the
draft fence, and early registry adoption allowed a later rename to strand
secondary settlement. Three additional executed regressions failed on that
candidate. Structural mutation guards, complete graph cardinality and delayed
public registration address those findings. Material review then identified that
the retained fence also rejected the real join-achievement forwarding path. Two
real Character/Guild-manager regressions failed, with empty and matching
catalogs. Character progress and deferred Guild effects now use the existing
effect-capture pattern separately. All finance fixtures now use real Character
and Guild achievement managers. A completing Guild achievement subsequently
reproduced missing Character completion feedback after the final snapshot.
Effects now drain before ordinary capture under narrow achievement admission;
pending effects prevent readiness. Public adoption/fence release is a one-time
transition. All22 focused and202 affected cases pass with zero failures/skips.
Spec and Standards final material reviews each have zero remaining findings;
there were no subsequent Source edits. The final owning WorldServer build took
25.00s, the test-only build13.20s, and final test bodies23s/51s. Scope includes
the existing guild creation/affiliation/achievement, housing fee/decor, atomic
save, row ownership and Character achievement-publication controls.

The latest20 all-state upstream PR heads/bases/states remain unchanged against
the prior reviewed inventory, including open608 and closed/unmerged609. The
concurrent research/label commits85efbcc5 andfedf5972 changed no Source; they were
preserved, and the final Source/binary binding was revalidated against the actual
publication parent. No full solution build/full suite ran. The unchanged
historical handoff Source-divergence result was retained without rerunning it.

## Remaining scope

Community creation couples residence state as well as guild membership; its
Service Token option additionally crosses Auth and Character databases. These
require their own complete transaction/handoff and recovery integration. The
existing public Community route is not claimed fixed by this ordinary Guild
milestone. Cross-owner Guild/Character achievement atomicity, configured-provider,
physical crash/restart, deployment/broker/socket
and native/retail qualification remain separate. Source/SQLite tests cannot
establish retail completeness. Preserve defaults15:1/17:1 and no automatic44 or
seven baseline entitlements; native client launch and Computer Use remain stopped.
