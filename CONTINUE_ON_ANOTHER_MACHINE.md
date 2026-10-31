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
Use the latest published commit on this branch; the chat handoff records its exact
SHA. Git is sufficient to resume source implementation and run the build/tests.

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
links. The checked-in checkpoint proof is a selected public subset. External
frozen binaries, full provider snapshots, historical harnesses and private runtime
material are not part of this Git-only handoff.

## Git-only continuation and external-evidence limits

Only this repository and the resume prompt are needed to continue source work.
No transfer archive is required or promised. The user cancelled bulk packaging
and requested a repository-and-prompt handoff.

Do not assume that source-machine `I:\GIT`, `I:\Wildstar`, MySQL schemas, cached
exports, tool installations, broker state or PID files exist on the destination.
Use the checkout's actual path. Ordinary build/tests use synthetic fixtures and
can run without recreating the old live stack. This was verified from a fresh
remote clone: full build zero warnings/errors and all10,277 tests passed.

For later native, content regeneration, MySQL or live-runtime qualification,
recreate the necessary isolated dependencies using the repository setup guides
and operator-provided client/reference data. External historical absolute links
may be unavailable. Keep affected evidence rows unverified until the actual
inputs are available; source tests do not substitute for missing native evidence.
The old report/SQL/client material was never pushed into the public repository.

The prior live stack ran separately qualified8378 binaries, not current code.
Do not import old process identities or assume copied-runtime migration parity.
Qualify current code on fresh isolated schemas and preserve entitlement policy.

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

Recreate the Nexus/`reverse-engineer-anything` skills and required analysis tools
on the destination when native work requires them. Do not copy old Codex
authentication or assume its configuration exists on the new machine. REA was pinned to **rea-agents@6.8.0**.
The prior supported Node runtime is **24.22.2**; Node25 was incompatible.

Use normal source tools for ordinary repository work. For shipped-artifact work,
follow the bundled REA skill and the repository evidence ladder. Cached exact-build
exports existed only on the source machine; local Ghidra project databases were
absent. Rebuild exports only when required, using the repo scripts and an
operator-provided client. Ghidra native
provider configuration/live debugger attachment was not established by the latest
source-only passes. Static exports do not prove a live attach.

On the destination machine, review the REA setup plan before editing its MCP
configuration. Typical commands after installing a compatible runtime and putting it first in PATH:

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
