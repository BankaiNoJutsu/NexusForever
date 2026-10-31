# World-location distance prerequisite

Question: what does live prerequisite type16 compare, including upstream #554's
unresolved threshold conversion?

## Native and shipped-table evidence

WildStar64.exe build16042 SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`:

- Live dispatcher `1404a2100`, case `0x10`, calls vtable offset `+0x278`.
  Slot `140b679b8` contains `14049e490`. This is the live route; the old
  sequential handler-table index41 attribution was incorrect.
- At `1404a3305`, `MOVD` loads raw value0 from `[rsi+0xc]`. At `1404a331b`,
  `CVTDQ2PS` converts signed int32 to float32. The fifth argument is stored at
  `[rsp+0x20]` before the call at `1404a3324`. It is neither an unsigned float
  conversion nor a reinterpretation of the raw bits as float.
- `14049e490` resolves objectId0 through `14024b980`, whose table-name anchor is
  WorldLocation2. Missing rows return false. Row worldId at `+0x28` must match
  the current world before any comparison, including NotEqual.
- Unit coordinates at `+0x11e0/4/8` minus row coordinates at `+0xc/10/14`
  produce the full float32 XYZ Euclidean distance. Radius and vertical bounds
  are unused. The first evaluated unit supplies the position, not the optional
  prerequisite target.
- Float comparator `1404a2010` supports raw modes1..6 as `==`, `!=`, `>`, `>=`,
  `<`, `<=`; unknown modes return false. IEEE NaN behavior is preserved.

The shipped tables contain154 type16 clauses across all three slots, including
five signed-negative thresholds (all raw `0xffffffff` with absent location0,
so they remain false). Of those clauses,103 reference a present
WorldLocation2 row;51 reference an absent row and remain false. For example,
Prerequisite2048 uses mode4, threshold50, location6956 in world22, with center
`(-5685.96484375, -998.722900390625, 115.67254638671875)`.

Exact raw bytes, table hashes, extracted witnesses and a reproducible reader
are external evidence under
`I:\GIT\NexusForever-assessment-report\remaining-goal\prereq16-*`.
Cached native fragments are the exact-address `1404a2100`, `14049e490`,
`14024b980`, and `1404a2010` fragments in the matching binary-hash directory.
No decompiled client body is copied into tracked source.

## Implementation and boundary

`PrerequisiteCheckDistanceToWorldLocation` registers at raw16 and supports both
the existing player API and the unit API. Missing subject/map/world/table/row
fails closed. The server uses the subject's current map world and position.
It preserves signed threshold conversion, single precision and all six native
comparisons. It does not use volume radius, integer floor or target substitution.

The old `Prerequisite_CheckZoneExplored` label at `14049e490` is corrected to
`Prerequisite_CheckDistanceToWorldLocation`. Type41's existing server behavior
is retained; correcting this mistaken native attribution supplies no evidence
for changing that separate runtime policy. Type95 is outside this slice.
Entitlement defaults, reward policy and existing prerequisite APIs are unchanged.

## Verification

30 regressions pass in the final full suite, including the actual2048/6956 witness through real
DI/manager dispatch for a player and NPC, six comparisons, missing contexts,
world mismatch, fractional vertical distance, signed thresholds, float32
rounding and IEEE NaN/infinity. Independent review found no production defect;
its requested nonzero-Z regression is included. Full solution build passes with
zero warnings/errors and all6,747 tests pass with none failed or skipped.
Logs/TRX are `prereq16-final-build.log`, `prereq16-final-tests.log`, and
`prereq16-tests/prereq16-final-suite.trx` under the external evidence root.
The corrected label was applied and is present in the fresh Ghidra functions
export (`prereq16-labeled-export/WildStar64.exe/functions.csv`).
Native UI verification of this newly implemented check remains unperformed.
