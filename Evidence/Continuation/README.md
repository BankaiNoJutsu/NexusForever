# Lean continuation evidence

This is the owner's selected lean, public text snapshot from 31 October 2026.
It supplements the readable assessment and PR triage in
`Decomp/Analysis/assessment-snapshot` and the existing durable RE notes. It does
not deploy anything or resume the paused implementation goal.

The complete store occupies approximately **6.5 MiB** and restores **56.6 MiB**
across **2,154 files**. Exact member byte counts are in `summary.json`.

It contains the latest realm-bank-splits/checkpoint10277 provider harnesses and
proofs, assessment/RE summary records, focused native traces and exports, retail
Contract reset evidence, and all seven custom Nexus/REA skill instruction folders.
The existing checked-in checkpoint already supplies the exact full/focused TRX,
negative controls, source manifest and build/verification logs; those large files
are not duplicated here.

From the repository root, using Python's standard library:

```powershell
python Tools\Handoff\Restore-Evidence.py --verify-only
python Tools\Handoff\Restore-Evidence.py
```

Restoration writes under ignored `.continuation-evidence/`, never to historical
absolute paths. Choose another location with `--output <directory>`, or restore
only `--category assessment`, `--category reverse-engineering` or
`--category agent-skills`. Existing identical files are accepted; different files
are never replaced. Nothing restored is executed or imported automatically.

Resolve an old link from the documentation with:

```powershell
python Tools\Handoff\Restore-Evidence.py --legacy-path "I:\GIT\NexusForever-assessment-report\ASSESSMENT.md"
```

`summary.json` records exact counts, byte lengths and manifest digests.
`index.jsonl.gz` maps each original path/SHA256 to its published SHA256, length,
redactions and content chunks. Chunks are deduplicated, individually gzipped and
stored in bounded `packs/*.pack` files; `object-index.json.gz` supplies offsets.
Pack, chunk and complete-file hashes are verified. `review.json` binds the
credential review to the published member index. No archive utility, external
storage, LFS download or transfer key is required.

Credential fields, literal connection/password/URI values and non-fixture emails
are redacted, with an additional comparison against parsed local configuration
credentials. A changed record is a **derivative**, not the byte-identical source:
validate restored content with `PublishedSHA256`. Original hashes embedded in old
proofs may disagree. Archive verification establishes integrity of published
history; it does not rerun provider tests or qualify a live server. Read and adapt
old harness paths, fixture credentials, build references and process/schema
assumptions before reuse.

Focused derived exports are an owner-authorized archival exception to the usual
workflow rule against committing raw exports. They remain separate evidence data,
outside implementation source. The bulk function cache and call graphs, raw
static SQL, mapping output CSVs, older test history, binaries and media are omitted.
`optional-datasets.json` inventories the omitted groups/source roots;
`exclusions.jsonl.gz` records original safety/format exclusions (a directory entry
applies to its entire subtree). Read [OPTIONAL_DATASETS.md](OPTIONAL_DATASETS.md)
and the repository-root `CONTINUE_ON_ANOTHER_MACHINE.md` before later native,
data-authoring or live-runtime work.

The source-machine publication utilities are `Publish-Evidence.py`,
`Select-Lean-Evidence.py`, `Review-Evidence.py` and `Pack-Evidence.py` under
`Tools/Handoff`. Selection is followed by full credential review before packing.
Packed snapshots are frozen; changing membership requires rebuilding loose
objects and reviewing the result again. These utilities are not destination
setup commands. Only `Restore-Evidence.py` is needed to use this archive.
