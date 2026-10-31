# Native percentage prerequisites: types 71 and 215

2026-10-29, following the remaining #595 combat review. The exact build-16042
client identity is recorded in `PREREQUISITE_COMPARISON_UPSTREAM_FIXES.md`.

The live dispatcher at `1404a2100` directly routes case `0x47` to `1404a4e30`
with subject, comparison, objectId and value. This helper calls current-vital
`140466520` and capacity-vital `1404667c0`, then compares current * 100 / capacity
through float helper `1404a2010`. Type 71 is therefore **VitalPercent**, not the
fork's old ClassProgress label. No runtime handler previously owned that label.

Case `0xd7` routes directly to `1404a4cf0`. It reads the subject's virtual
current/capacity slots `+0x70/+0x78`, also used by both vital helpers for selector
3 (ShieldCapacity), and performs the same percentage comparison. Type 215 is
**ShieldCapacityPercent**; its former Shield215 label had no runtime handler.
No orphan handler-array inference is used for either mapping.

Both handlers use the evaluated subject. They preserve single-precision
arithmetic, fractional percentages and values above 100. Native float
comparisons have IEEE behavior: zero/zero yields NaN (NotEqual succeeds; ordered
comparisons and Equal fail), and positive/zero yields positive infinity.
Health and shield use their actual raw capacities, including zero. Unsupported
subjects/pools fail closed; other supported resource pools use the existing
positive-capacity runtime API. Native class-pool fallback aliases and zero
capacities outside health/shield remain unmapped, and no resource defaults are
changed to invent support for them.

Regression cases cover raw comparison IDs, zero capacities, distinct subject
and target values, correct vital selection, ignored shield objectId, fractional
and over-capacity values, large uint values, keyed registration, missing context
and unsupported capacities. Existing type73 raw vital and all old target-aware
handlers retain their behavior. Unit-context integration uses the same subject
helpers through the compatible API extension owned by the #568 slice.

Native evidence is in the recovered exact-hash cache and the follow-through
report's focused native exports. This evidence establishes implementation
semantics; it does not establish a native-client gameplay smoke result.
