# Continue on another machine

This handoff records the 31 October 2026 Zurich checkpoint. The implementation
goal is unfinished and was paused for this handoff. No current-source runtime
deployment was performed. Read this file before starting services or migrations.

## Get the code and verify the checkpoint

```powershell
git clone --branch legacy-di-storefront-hardening --single-branch https://github.com/BankaiNoJutsu/NexusForever.git I:\GIT\NexusForever-assessment
cd I:\GIT\NexusForever-assessment
python Tools\Handoff\Verify-Continuation.py
dotnet restore Source\NexusForever.slnx
dotnet build Source\NexusForever.slnx --no-restore -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo
```

The runtime implementation commit is `71cde855988446c304341f906556542d6868c412`.
The final pushed handoff commit and offline Git bundle are recorded in the transfer
folder's `TRANSFER.md` and `release.json`, avoiding a self-referencing commit hash.
Use the final commit from that record when exact reproducibility is needed.

The verifier compares 5,888 source/tool files through Git's newline filters, then
checks the copied historical test evidence. This accommodates the repository's
CRLF C# checkout policy. It does not turn old PID/schema observations into current
live verification. Historical external `verify.py` scripts bind old HEADs, absolute
paths, schemas and process identities; do not rerun them unchanged and interpret
their expected machine mismatch as a code regression.

The build used .NET SDK **10.0.300**, .NET 10/C#14 and Python **3.13.14** on Windows.
Latest qualified implementation checkpoint **10277** has 10,277 full tests, 207
focused checks, 1,796 MySQL cases (530 configured-strategy cases), four additional
Auth race probes, four provider model gates, 31 compatibility checks and 726
actual Contract quote choices. Full build: zero warnings/errors. Contract fixture
item/stacking metadata remains synthetic. These results do not claim retail parity.

## Read these records

- [Portable assessment snapshot](Decomp/Analysis/assessment-snapshot/ASSESSMENT.md)
  and [complete PR triage](Decomp/Analysis/assessment-snapshot/PR-TRIAGE.md).
- [Remaining-work ledger](Decomp/Analysis/ASSESSMENT_REMAINING_WORK.md),
  [current status](CURRENT_STATUS.md) and
  [missing-feature matrix](Decomp/Analysis/MISSING_FEATURE_MATRIX.md).
- [Latest bank evidence](Decomp/Analysis/REALM_BANK_CODECS_AND_LOADING.md).
- [Contract reset evidence](Decomp/Analysis/CONTRACT_RETAIL_RESET_EVIDENCE.md),
  [account/session synchronization](Decomp/Analysis/CONTRACT_ACCOUNT_SESSION_SYNC.md)
  and [claim handoff](Decomp/Analysis/CONTRACT_CLAIM_TRANSACTION_AND_ACCOUNT_HANDOFF.md).
- [RE continuation guide](Decomp/Analysis/CONTINUATION_GUIDE.md),
  [setup](Tools/Setup/README.md), [data imports](Tools/DataMapping/sql/README.md)
  and [load harness](Tools/NexusForever.LoadTest/README.md).

The assessment snapshot is copied from the external report, retaining historical
links. Restore the private transfer package to recover their full targets. The
checked-in checkpoint proof is a selected public subset; frozen binaries,
provider snapshots, historical harnesses and private runtime material are in the
encrypted transfer package, not Git.

## Restore the private material

The separately supplied `NexusForever-handoff-20261031` transfer folder contains
encrypted archives, their SHA256 manifest, an offline Git bundle, SQL snapshots,
client assets, decompile caches, source-data dumps, agent skills and tool runtimes.
`TRANSFER.md` describes the exact contents, extraction, key location and limits.
Keep the decryption key separate and do not commit the decrypted payload.

Preserving the original `I:\GIT`, `I:\Wildstar` and referenced `I:\Videos` paths
minimizes changes to historical harnesses. On a machine without drive I:, a new,
empty workspace can be exposed with `subst I: D:\NexusWorkspace` after verifying
that I: is unused. This is an operator action, not part of the verifier.

Database dumps contain private accounts and characters. Restore them only into a
fresh isolated MySQL instance after inspecting the schema list. Dumps include
CREATE/USE/DROP TABLE statements. They do not copy MySQL users/grants; recreate
an appropriate local user and configure credentials privately. Historical proof
scratch databases are represented by retained snapshots/harnesses, not all SQL
schemas. Never import these dumps over unrelated databases.

The old live stack used separately qualified **8378** binaries with nine services
and six drained queues. It did **not** run current code/schema. Its copied configs,
assets and binaries are historical reference material. PIDs and start times are
machine-specific. Do not adopt old owner files, attach to assumed PIDs, launch
colliding services, or migrate its restored database just to make a health check
green. Current code includes later bank/Contract/account migrations; populated
upgrade proofs are retained, but copied-runtime deployment/native qualification
remains work. Original runtime/reference data and legacy Characters must stay intact.

## Constraints and next work

1. Preserve entitlement defaults **15:1 / 17:1**. Do not auto-grant 44 or the
   seven proposed baseline character entitlements.
2. Start with periodic bank dirty-field/expiry persistence. `Inventory.Save`
   excludes the realm bank; countdown/other dirty fields require a coordinated
   durable path. Current single writes, owned deletion, swaps, consumed merges and
   splits are qualified; automatic recovery and post-commit publication remain.
3. Continue public WorldServer **07CE** Contract claim integration, retail cycle
   reset/unclaimed/excess rules, actual full-table delivery, track35 earning,
   deeper lifecycle/load/copied-upgrade/socket/native qualification and all broader
   assessment/upstream/AMP/combat/content requirements. Public claim routing is
   currently disabled. Do not enable it merely because the internal path passes.
4. Retail reset is the user's requested policy. Evidence suggests final reward
   collection resets the track, but unclaimed/excess edges are not established.
   Search primary/retail evidence when uncertain; do not ask the user to invent policy.
5. All 31 content CSVs / 167,484 rows remain `not_retail_complete`. The three
   restored tutorial videos validate. The June19 Q5597 historical UI bundle is
   unavailable; do not ask for it again or relabel its replacement worksheet as UI proof.
6. Computer Use and native WildStar launch remain stopped until explicitly resumed.
   The I-drive client previously loaded and responded normally against the old
   isolated stack; this does not validate current source. Do not automate login.
7. Preserve unrelated work and original data. Do not push additional implementation
   changes merely because this handoff was explicitly pushed. Read `AGENTS.md` and
   applicable skills before work; no agent delegation is presently authorized.

## Reverse engineering tools

The private payload includes the Nexus skills, `reverse-engineer-anything`, a
compatible Node24 runtime and the available portable decompile toolchain. Restore
skills under the new user's agent skill directories; do not copy the old Codex
configuration or authentication files. REA was pinned to **rea-agents@6.8.0**.
The prior supported Node runtime is **24.22.2**; Node25 was incompatible.

Use normal source tools for ordinary repository work. For shipped-artifact work,
follow the bundled REA skill and the repository evidence ladder. Cached exact-build
exports are available; local Ghidra project databases were absent. Rebuild only
when required, using the repo scripts and the supplied client. Ghidra native
provider configuration/live debugger attachment was not established by the latest
source-only passes. Static exports do not prove a live attach.

On the destination machine, review the REA setup plan before editing its MCP
configuration. Typical commands with the compatible runtime first in PATH:

```powershell
npx -y rea-agents@6.8.0 doctor --client codex --json
npx -y rea-agents@6.8.0 setup --client codex --dry-run --json
```

Apply reviewed configuration only when authorized, restart/reconnect Codex, then
verify an actual REA tool request. A doctor/setup success alone is not live MCP
verification. Broker/service installation, fresh DB users and machine-specific
paths must likewise be reviewed rather than copying old credentials blindly.

## Resume prompt

```text
Continue the unfinished NexusForever assessment/implementation goal in this clone
on legacy-di-storefront-hardening. Read CONTINUE_ON_ANOTHER_MACHINE.md,
Decomp/Analysis/assessment-snapshot/ASSESSMENT.md, PR-TRIAGE.md and the linked
remaining-work ledger. Verify the published source with Tools/Handoff/
Verify-Continuation.py, then create/resume the goal explicitly in this chat.
Preserve all fork functionality, entitlement defaults15:1/17:1/no44 and original
data. Begin with periodic bank dirty/expiry persistence, then complete all
remaining assessment requirements and continue evidence-backed implementation
using reverse-engineer-anything and CONTINUATION_GUIDE.md. Add meaningful
regressions and run full build/tests after coherent slices. Keep retail evidence,
native/deployment limits and all broader requirements explicit. Computer Use and
native launch remain stopped until explicitly resumed. Do not push without a new
request. Do not claim the goal complete from a partial checkpoint.
```
