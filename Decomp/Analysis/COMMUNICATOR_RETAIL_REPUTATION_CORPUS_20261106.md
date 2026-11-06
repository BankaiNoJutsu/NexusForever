# Communicator 609: retail reputation corpus (2026-11-06)

Input: build 16042 `DB\CommunicatorMessages.tbl` from the installed client (4,105 rows, 494,256 bytes,
SHA-256 `086346547b6e5f457ff25d121b5c555bf708a69a40b49db34c6870b18d137a75`), extracted with
`Tools/ArchiveProbe` ([RAW_TABLE_EXTRACTION_20261106.md](RAW_TABLE_EXTRACTION_20261106.md)).

The ledger's 609 entry named a 481759-byte table with SHA-256 `6a46e0e5...`; no extracted table has that
size or hash, so that identity stays unreconciled. The 494,256-byte table here is the corpus used instead.

## Census

- 23 of 4,105 rows carry a reputation condition (`FactionIdReputation != 0`); no row has a nonzero
  `ReputationMin/Max` without a faction.
- Bound shapes: 8 rows with both bounds zero (factions 166/167), 5 rows with a nonzero minimum and zero
  maximum (231 and the top tier of 533/609), 10 rows with both bounds nonzero. No row has maximum < minimum.
- Factions 533 and 609 are contiguous tier bands: 2000-3999, 4000-7999, 8000-15999, 16000-31999, 32000-open.
- Therefore 13 of 23 retail rows depend on zero-bound handling. The current predicate
  (`CommunicatorMessage.Evaluate`, change f791dc37) treats a zero bound as unbounded and compares nonzero
  bounds inclusively, which is the native static cross-check's reading.

## Test

`CommunicatorRetailReputationRowsTests` (3 cases) pin the 23 rows (id, faction, min, max) and evaluate each at
and around its boundaries. They passed on first run, so no defect was found; 124 Communicator cases pass in
the same filter. Evidence tier: Source/SQLite plus raw-table corpus. This corroborates the table side of the
609 reading; native timing, UI and runtime/sniff behavior remain unqualified, and the finite/nonfinite
policy for NaN and infinities is unchanged and still not retail-verified.
