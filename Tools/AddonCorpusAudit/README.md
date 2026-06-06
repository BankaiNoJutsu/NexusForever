# Addon Corpus Audit

`audit_addon_corpus.py` scans a local folder of WildStar addon `.zip` archives
and writes repeatable evidence reports for Apollo events, public client APIs,
and broad subsystem categories.

The default corpus path is `I:\Wildstar Addons`, matching the local evidence
passes documented under `Decomp/Analysis`. Pass `--addon-dir` when using a
different archive location.

## Refresh Reports

```powershell
python Tools\AddonCorpusAudit\audit_addon_corpus.py `
  --addon-dir "I:\Wildstar Addons" `
  --output-dir Tools\AddonCorpusAudit\reports
```

The generated report set is intentionally compact enough to commit. Keep raw
addon archives and exploratory scratch output under `artifacts/` or another
local-only path.
