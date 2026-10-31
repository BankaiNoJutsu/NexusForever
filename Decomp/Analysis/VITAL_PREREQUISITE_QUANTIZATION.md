# Raw vital prerequisite73 quantization

Question: does type73 compare the float resource directly with its threshold,
or quantize it before the unsigned comparison?

## Mapped evidence

Exact build16042 WildStar64.exe SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`:

- Live dispatcher `1404a2100` case0x49, jump slot `1404a4968`, reaches
  `1404a23cb`. A missing first subject returns false through `1404a23d4/7`.
- Current-vital helper `140466520` returns the selected float in XMM0. Resource1
  (selector6) is an actual `MOVSS` load at `1404665a9`, so fractional input is
  supported; a decompiler integer-looking return type is not authoritative.
- `CVTTSS2SI r8,xmm0` at `1404a23ed` truncates to signed int64. Helper
  `1404a2090` compares only low32 bits as unsigned with raw value0. The
  threshold is not converted to float.
- Masked invalid conversion of NaN/infinity/out-of-int64-range values returns
  int64 minimum and therefore low32 zero. The shared server conversion explicitly
  accepts `-2^63 <= value < 2^63`, then truncates and preserves low32 bits.
  This avoids the CLR's different out-of-range saturation policy.

The actual Prerequisite table has235 clauses across195 rows for type73,
including141 Resource1 clauses. There are no signed-bit threshold values.
Row5985 is Resource1 >300. Row5986 is Resource1 <=300 AND >200.
For current resource300.5, native5985 is false and5986 is true; the previous
direct float comparison returned the opposite boundary results.

External exact bytes and all raw rows are retained under
`I:\GIT\NexusForever-assessment-report\remaining-goal`:
`prereq73-native-case.json`, `prereq73-table-witnesses.json`,
`PREREQUISITE73_QUANTIZATION_PROPOSAL.md`, and the independent review.
Cached exact-address native fragments `1404a2100`, `140466520` and `1404a2090`
remain the decompiler context. No decompiled body is copied into tracked source.

## Implementation boundary

`PrerequisiteCheckVital` quantizes the value returned by the existing
`TryGetVitalValue` API and retains all six comparison modes, unknown-mode
logging, existing pool mappings and unsupported-pool failure. Missing subjects
fail closed. Player and NPC subject ownership remains unchanged; the optional
target does not substitute for the evaluated unit.

The shared `PrerequisiteCompare.TruncateNativeScalar` also serves type95's
already mapped signed64/low32 conversion. It does not change type95's sum order,
threshold squaring or missing-target policy. Percentage checks71/215 retain
their separate float comparisons and IEEE behavior. This slice does not claim
new support for unmapped native vital selectors, change entitlement defaults,
or implement Challenge progression.

## Verification state

27 new regressions cover the shipped5985/5986 boundary, all raw comparison
modes, signed resource truncation/wrap, finite and invalid conversion limits,
raw uint thresholds beyond float32 exact precision, missing contexts and raw73
keyed registration with player/NPC subject APIs. Independent review found no
production defect. All27 cases pass in the final full6,923-test suite with zero
build warnings/errors (`seed-defaults-tests/seed-defaults-full-suite.trx`).
The owned runtime is frozen6923; its login/load gate does not constitute a
native vital-prerequisite UI/cast scenario.
