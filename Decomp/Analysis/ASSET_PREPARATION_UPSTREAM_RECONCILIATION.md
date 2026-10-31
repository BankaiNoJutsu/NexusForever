# Asset preparation and Windows validation

2026-10-29, reconciling upstream #570 while retaining the fork's Windows,
SQLite, filtered-generation and bounded-parallelism workflows.

MapGenerator `--prepare` now locks an output root, validates an existing cache
and its SHA256 inventory, or generates replacements in a unique staging child.
It loads all runtime-required table models/locales and fully parses generated
map files before promotion. The incomplete marker remains until the resulting
map set, including retained custom maps, and inventory validate. Failed
generation leaves original assets intact; a failed promotion remains visibly
incomplete. This is recoverable staging, not a filesystem-wide atomic swap.
Rerun preparation after failure. Custom assets and unrelated `.prepare`
directories are retained. Filters cannot certify a complete prepared cache.

Read-only `--validateTables` and `--validateMaps` modes support Windows setup.
The launcher no longer treats `World.tbl` or four filenames alone as proof of
usable inputs. It validates table loading, map build/version and complete map
streams and rejects an incomplete preparation marker. Repo-root default
resolution occurs after parameter binding for Windows PowerShell 5.1.
The validation smoke imports only the three validation functions by AST;
it never executes the launcher's service, database or client actions.

Aspire's separate, default-disabled preparation resource and successful-exit
waits are documented in `ASPIRE_ASSET_PREPARATION_UPSTREAM_FIXES.md`.
The full Docker-hosted Aspire startup flow remains unperformed on this host.

## Verification

- Regression cases cover staged failure, invalid output, cache reuse without
  client archives, corruption/missing files, filtered generation, exclusive
  ownership, unsafe inventory paths, wrong-build/truncated maps and preservation
  of custom files. A corrupt retained custom map keeps preparation incomplete.
- Windows validation smoke passes under PowerShell 7 and Windows PowerShell
  5.1, including real client tables and invalid/missing/required map fixtures.
- A real `--prepare` run against `D:\WildStar\Patch` completed successfully:
  **384 TBL files, two locale files and 825 map files**. All 1,211 generated
  files are inventoried by SHA256. A second run with no client path reused
  the verified cache successfully. The final table validator also loaded the
  actual extracted tables through runtime models successfully.
- Outputs remain outside Git at
  `I:\GIT\NexusForever-assessment-report\remaining-goal\full-prepared-assets`.
  Logs: `full-asset-preparation.log`, `full-asset-cache-reuse.log`,
  `full-model-table-validation.log`, and the two Windows validation smoke logs.

These gates establish preparation and file readability. They do not establish
retail collision, pathing, scripted gameplay or native-client presentation.
