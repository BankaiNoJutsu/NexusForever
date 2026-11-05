# Housing decor purchase settlement

The public `ClientHousingDecorUpdateHandler` create route now queues paid and
free decor purchases on the original registered personal or community residence.
The command copies the accepted decor fields and price instead of retaining the
mutable request. Before preparation it rechecks the original map, Player/session,
membership, residence permissions, funds and existing placement rules.

The complete frozen Character save, selected fee and proposed residence decor
row commit in one serializable Character unit. Existing dirty residence work
settles first. The proposed decor stays outside the live collection until the
unit and its ordinary followup reconcile. A typed receipt checks the exact SQL
residence owner and community decoration permission, and authorizes only the
new captured decor row plus unchanged rows in that root. It reuses the existing
receipt store; no schema or migration changes are introduced. Proven rollback
retains the same intent, while lost acknowledgement reconciles the committed
receipt instead of charging or creating decor twice. Auth keeps its existing
preceding save; distributed Auth/Character atomicity is not established.

Character achievement progress joins the primary frozen save. Guild achievement
forwarding is captured separately and waits for proven primary commitment. The
primary phase ends before those callbacks run; a rejected callback retains its
cursor for world-update retry before the ordinary Character followup. Retired
guilds and replaced achievement managers are skipped. Ordinary achievement
callers retain immediate forwarding. These callbacks are retained in memory;
durable cross-guild replay across a physical restart remains a separate gate.

The world update adopts the committed decor and releases wallet notices and
decor broadcasts through retained cursors. Original root/map, recipient and
session guards suppress stale publication. An enqueue rejected before acceptance
resumes at the failed recipient with a new packet, without another debit or decor
insert. Existing residence operations wait for the prepared purchase owner.

## Verification

The [portable report](coverage/housing-decor-purchase-20261105.json) binds the
reviewed Source inputs, owning/test/provider DLLs, scoped runs, configured MySQL
cases, upstream refresh and owned-runtime shutdown. Local evidence lives under
`artifacts/continuation-20261105/housing-decor-purchase/`.

The original compiled baseline demonstrates the early wallet debit through the
real public handler. Eleven new cases cover personal/community purchases, either
SQL half rejecting, lost commit acknowledgement, publication retry, stale actor,
immutable request/price, SQL permission loss, real Character/Guild achievement
rollback and rejected postcommit guild forwarding. Final Source checks pass
63 decor/rename/achievement cases and seven existing map controls. An earlier
159-case housing/achievement pass preceded the committed-effect retry correction
and retains that candidate boundary. Both final review axes report zero remaining
findings after correcting premature guild progress and the callback retry wedge.

Configured MySQL8.4.13 passes all12 final cases (11 new decor cases and one
unchanged rename control), with configured retries and49 applied Character
migrations per disposable schema. The initial11-case candidate remains separate
from this final gate. All23 intermediate/final schemas were dropped successfully;
the single owned server shut down gracefully and its listener/runner/helper are
confirmed absent. The Game, Character database, WorldServer and test DLL copies
match between owning outputs, tests and the final provider runner. Final owning
build:12.99s; test compile:19.87s;63-case scoped execution:38s; final MySQL
runner:199.343s. These are observed qualification durations, not performance claims.

Scoped builds/tests preserve the user's validation policy. No full solution or
full suite was run. Trigger and interceptor faults establish logical recovery;
they do not establish physical failure, deployed restart/load, socket/native or
retail behavior. Native client launch and Computer Use remain stopped. Defaults
15:1/17:1 remain unchanged, with no automatic44 or seven baseline Character grants.
