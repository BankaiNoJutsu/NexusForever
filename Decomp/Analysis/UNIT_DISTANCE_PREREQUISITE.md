# Unit-distance prerequisite95

Question: does live type95 round ordinary distance down, as the current server
does, or compare a different quantity?

## Native mapping

Exact build16042 WildStar64.exe SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`:

- Live dispatcher `1404a2100` case `0x5f` resolves through jump slot `1404a49c0`
  to inline code `1404a2f9d`. The old label at `14049f770` came from an invalid
  sequential table-index assumption. Its actual object-tree/payload-presence
  body is unrelated to this live distance route; its domain remains unknown.
- At `1404a2fc4`, `IMUL r9d,r9d` squares the raw threshold modulo2^32.
  `SUBPS`/`MULPS`, followed by `ADDSS` at `1404a2fe0` and `1404a2fe4`, compute
  `(targetX-subjectX)^2 + (targetY-subjectY)^2 + (targetZ-subjectZ)^2` using
  left-to-right float32 addition. There is no square root.
- `CVTTSS2SI r8,xmm1` at `1404a2fe8` truncates the float sum to signed int64.
  Comparison helper `1404a2090` reads only the low32 bits in `r8d` and compares
  them as unsigned against `r9d`. Its six comparison IDs retain their existing
  mapped meanings; unknown IDs return false.
- With normal masked invalid conversion, NaN/infinity/out-of-int64-range inputs
  yield int64 minimum, whose low32 bits are zero. The shared scalar helper
  explicitly checks the signed-int64 range `-2^63 <= value < 2^63` before
  converting; squared distance is nonnegative. Modern CLR casts saturate and
  would otherwise produce a different low32 result. This preserves the native
  conversion independently of the host CLR's out-of-range cast behavior.
- Missing subject returns false. With a subject but no target, only raw mode2
  (NotEqual) returns true through shared branch `1404a219b`. Neither objectId
  nor a world-map comparison is used by this live case.

The shipped Prerequisite table has59 type95 clauses across49 rows, using all
six comparison modes and thresholds3..128. Five clauses have nonzero objectId,
which this case ignores. Row5814 uses mode6/threshold50. Row6261 combines
Equal10 with NotEqual5; row8014 combines LessThanOrEqual30 with GreaterThan25.
The ordinary threshold range does not exercise integer square overflow, but the
raw handler contract still defines it.

Reproducible evidence is external under
`I:\GIT\NexusForever-assessment-report\remaining-goal`:
`prereq95-evidence.py`, `prereq95-native-case.json`,
`prereq95-table-witnesses.json`, and `PREREQUISITE95_NATIVE_REVIEW.md`.
Exact-address cached fragments `1404a2100`, `1404a2090`, and `14049f770` retain
the corresponding decompiler evidence. No client body is copied into source.

## Implementation and compatibility

The existing type95 handler retains its player/unit APIs and target ownership.
It now compares the mapped truncated squared quantity, handles missing contexts
as the native branch does, and preserves native uint32 arithmetic/conversion.
For example, distance5.1 with threshold5 previously compared5 against5; the
client compares26 against25. Distance5.01 compares25 against25 after squared
truncation, so a pure float-distance comparison would also be incorrect.
Existing exact3/4/5 player and cross-target NPC checks are retained.
Type16 world-location distance is a separate float/square-root contract.
Entitlement defaults and reward policy are unchanged.

## Verification state

The initial focused gate passes33 cases (31 new plus two existing player/NPC
compatibility cases). Independent review found no production defect and asked
for three additional boundary controls: left-to-right float32 addition,
unsigned low32 comparison at2^31, and invalid conversion at exactly2^63.
Those controls are now present:34 new cases pass in the full6,923-test suite
with zero build warnings/errors. `seed-defaults-tests/seed-defaults-full-suite.trx`
is the final result. The corrected object-tree
label is applied in `prereq95-labeled-export/WildStar64.exe/functions.csv`.
The first full run's two unrelated timing/SQLite teardown failures are retained
in `prereq95-full-tests.log` and `prereq95-tests/prereq95-full-suite.trx` and are
resolved by scoped SQLite pool cleanup and the isolated deadline-test collection,
with timeouts unchanged; two full6,918 runs and the final6,923 run pass.
Native UI validation remains unperformed; the owned live stack is frozen6923.
