# Armor-pierce and mitigation static evidence

## Finite cosine helper and the rating curve

5 November 2026. The previously unresolved call at `0x140453beb` targets
`0x1408fc950`. Its observed exception-directory group has one part,
`[0x1408fc950, 0x1408fcc50)`, with no chained fragments. The PE still matches
SHA-256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
REA's portable import inspection retains Evidence
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`;
its complete 19-DLL/384-function inventory contains no named cosine import.
LLVM supplies the instruction observations separately from REA. Neither tool
executes the client. Ghidra remains unconfigured; no unavailable deep-analysis
operation or setup was repeated.

The sampled positive finite path implements float-result cosine. Up to pi/4, it
starts at `0x1408fcb70` with `1 - x*x/2` and an even polynomial whose next
coefficients match `1/24`, approximately `-1/720`, `1/40320` and `-1/3628800`.
For the rest of the sampled range it reduces by pi/2 using the constant
`2/pi` at `0x1409c7fc0`, selects the sine polynomial at `0x1408fcb25` for an
odd quadrant, then adjusts the sign. This coefficient/branch interpretation
agrees with the [NIST cosine and sine series](https://dlmf.nist.gov/4.19).
This is an analyst identification, not a recovered symbol or a complete audit
of the large-argument and nonfinite helper calls.

A read-only analysis reconstruction follows the observed positive finite paths
over `[0, 1.5707963705062866]`, the rounded single-precision half-pi used by the
rating routine. Its 4,101 selected inputs, including adjacent floats around
pi/4 and the endpoint, agree with independently evaluated float-rounded cosine
within one float ULP. The reconstruction rejects NaN, infinity, negative and
out-of-range inputs explicitly. Those rejections describe the analysis scope,
not the executable's behavior. The endpoint cosine is approximately
`-4.3711388e-8`; do not replace target rounding with an idealized positive limit.
These are reconstruction checks, not native runtime observations.

The shared rating curve can therefore be described schematically with slope
`a`, linear base `b`, join value `c`, rating `r` and rolloff argument `k`:
`t = (c-b)/a`; below `t`, `v = a*r+b`; otherwise, with `e = r-t`,
`v = c + a*e*cos((e/(e+k))*(pi/2))`.
The routine then scales by the stored float approximation to 0.01, adds a
supplied offset and applies an upper cap. The executable rounds its scalar
operations at each single-precision instruction; the expression is not an
exact real-number or bit-identical replacement. The bounded cosine argument
requires finite nonnegative excess and a positive rolloff argument. Actual
GameFormula coefficients and reachable rating ranges remain unverified.

Disposition: **Mapped finite-domain cosine and symbolic rating curve**. No
combat implementation changed. The callers previously identified below still
resolve one unit for stat display. The required next evidence is the actual
coefficient/loader binding and independent pairwise damage/mitigation evidence
for attacker pierce, victim armor, order, caps and rounding. This closes the
unresolved-callee question without qualifying #557's remaining combat change.
No label export is claimed while the deep provider is unavailable.
`function_labels.csv` and `mapping_artifacts/function_evidence_inventory.csv`
are intentionally unchanged: the inventory links durable applied labels, and
this pass cannot apply/re-export a new label. This focused record and portable
result retain the mapped helper until that workflow is available. Other mapping
CSVs are intentionally unchanged because no new opcode, packet layout, structure
field, Source implementation or live evidence bundle is established. No packet
fixture is inferred; actual coefficient and pairwise/runtime blockers remain above.

The [portable result](coverage/armor-rating-math-20261105.json) binds PE and LLVM
identity, the observed unwind group, raw constants, full instruction extracts,
analysis reconstruction and review. Local evidence is under
`artifacts/continuation-20261105/armor-rating-math/`. Source and Tools are
unchanged; no C# build or test suite was needed. Native client/CUA remain stopped.

## Direct callers and the rating curve

4 November 2026. REA reopened the same build16042 PE with the SHA256 below.
A scan of `.text` for E8/E9 relative branches found two candidates targeting
`0x140461df0` (pierce), six targeting `0x140464240` (mitigation) and 21
targeting `0x140453b60` (shared rating conversion). LLVM disassembly from the
start of each enclosing exception-directory range confirmed all 29 as direct
calls at instruction boundaries. The two pierce calls are the getters listed
below. All 21 calls into the shared conversion are in the
`0x140460670`–`0x140465e1f` helper cluster.

The three additional mitigation callers are rating-percent getters. The
literal string and adjacent body pointer at each cell identify the binding.

| Method | Pointer cell | Body | Mitigation call |
| --- | --- | --- | --- |
| GetPhysicalMitigationRatingPercent | `0x140c5abf0` | `0x14064dc30` | `0x14064dc76` |
| GetTechMitigationRatingPercent | `0x140c5ac20` | `0x14064df40` | `0x14064df86` |
| GetMagicMitigationRatingPercent | `0x140c5ac50` | `0x14064e250` | `0x14064e296` |

Each resolves one unit and passes two sums to the mitigation helper. It then
subtracts the two terms of the second sum from the result before scaling for
display. Those terms are read from `+8fc` and the school-specific offset
(`+a44`, `+a4c` or `+a54`) on the same unit. All six direct mitigation callers
are stat-display bindings.

The shared conversion compares rating with a threshold derived from its
arguments. Below the threshold, it multiplies rating by the slope and adds
the base. At or above it, the excess rating enters a ratio of the form
`excess / (excess + argument)`. The routine passes that ratio times half pi
to `0x1408fc950`, then multiplies the return value by the excess and slope
and adds the input value that the linear branch reaches at the threshold.
Both paths scale by approximately 0.01, add another argument and apply an
upper cap. The above-threshold path uses nonlinear arithmetic and an
unresolved callee. Establishing the response curve and its relationship to the
fork's linear combat calculation requires the callee semantics, actual table
coefficients and combat observations on both sides of the threshold.

The scan also found zero literal 64-bit references to the three entry
addresses in file-mapped sections. Coverage is limited to those literal
references and E8/E9 direct branches. Computed or inlined paths remain
unresolved, as do attacker–victim ordering, coefficient provenance and
retail application. The #557 reconciliation remains at the boundary below.
Local evidence is under `artifacts/continuation-20261104/armor-callers/`,
including `rea-input.json`, `caller-evidence.json`, `formula-evidence.json`,
both inspection scripts and the disassemblies.

## Initial getter trace

2 November 2026 continuation. Disposition: mapped stat-display paths; combat
ordering remains blocked. No server behavior or Source/Tools input changed.
Computer Use and WildStar launch remain stopped.

The existing REA PE evidence in `native-static/rea-pe-input.json` binds the shipped
build16042 WildStar64.exe to SHA256
231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5.
This pass reuses that identity and the existing read-only LLVM22.1.6 workflow;
its disassembly is external inspector evidence, not a REA decompiler dossier.
Ghidra remains unconfigured; no unchanged unavailable-provider call was repeated.

Literal method-name strings and adjacent code pointers identify these bindings.
Each selected body has one observed exception-directory range, with no chained
parts or unsupported unwind metadata. Those ranges do not establish every
possible tail-called or cold semantic block.

| Method | Pointer cell | Body range, end exclusive | Observed calculation route |
| --- | --- | --- | --- |
| GetArmorPierce | 140c5a9f0 | 14064c090–14064c271 | Resolves one unit and calls140461df0 at14064c111; scales the result for display |
| GetArmorPierceRatingPercent | 140c5aa10 | 14064c2e0–14064c357 | Calls140461df0, subtracts that same unit's float at+994, then scales for display |
| GetPhysicalMitigation | 140c5abd0 | 14064d9b0–14064dbc1 | Calls140464240 with sums from one unit: +714/+694 and+8fc/+a44 |
| GetTechMitigation | 140c5ac00 | 14064dcc0–14064ded1 | Same helper, with+714/+69c and+8fc/+a4c |
| GetMagicMitigation | 140c5ac30 | 14064dfd0–14064e1e1 | Same helper, with+714/+6a4 and+8fc/+a54 |

The pierce helper140461df0 loads floats+994/+79c from its one unit, requests row
0x4f5 through140200220, selects+3c or fallback+38 as a scalar, and calls the common
formula routine140453b60 at140461fa9. The mitigation helper140464240 requests row
0x4d2, uses the corresponding scalar from its one unit and the supplied sums,
calls140453b60 at1404643f5, then applies a zero lower bound. Row identities1269/
1234 agree with the fork's existing rating/formula selections; the field meanings,
actual coefficients and complete loader binding are not newly verified here.

The shared routine140453b60–140453c45 contains a branch and arithmetic on supplied
scalars, with a call at140453beb to1408fc950. The 2 November trace left that
callee and the shared routine's other callers open. The observed API routes provide
one unit's displayed stats. They do not expose an attacker/victim pair, demonstrate
an attack application, or prove when piercing reduces effective victim armor.
No inference that the client lacks such a path follows from this selected scope.

## Upstream and implementation boundary

The fresh two-file [#557 diff](https://github.com/NexusForever/NexusForever/pull/557/files)
is pinned to4bda9d1269fda8464c8cf7686ff3743b6edeae91 in the returned file URLs.
It proposes reducing victim armor by the attacker's rating-derived pierce before
mitigation, corrects the physical offset and subtracts strikethrough from deflect.
The fork already has the physical-offset and strikethrough behaviors, including
its existing guards. The attacker-pierce step is still absent from
GetDamageAfterArmorMitigation. Copying that remaining step would exceed this
static display evidence; no duplicate implementation or combat-policy change was
made. The empty added upstream `bash` file has no behavior to reconcile.

Required next input: the complete pairwise damage/mitigation path and independent
actual-table, combat capture or runtime corroboration of pierce scope, ordering,
offsets, caps and rounding. Do not infer these from the names or rating-percent
display alone. This is not retail combat qualification.

Evidence under `artifacts/continuation-20261031/native-static/`: retained REA
input and input-hashes records, `upstream557-files.json`,
`find-armor-bindings.py`, `armor-string-bindings.json`,
`inspect-armor-chains.py`, `armor-chains.json`, ten selected disassemblies and
`armor-verification.json` and `armor-review-reports.md` (Spec0/Standards0).
Existing observations remain unchanged. Source5901ec17
keeps its separate provider checks and failed full gate in
[the qualification record](DESTINATION_PROVIDER_QUALIFICATION.md).
