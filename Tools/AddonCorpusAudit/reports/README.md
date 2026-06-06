# Addon Corpus Audit Reports

Committed snapshots from `Tools/AddonCorpusAudit/audit_addon_corpus.py`.

## Policy

- **Track here:** compact addon corpus reports that summarize Apollo events,
  client API names, addon inventory, and broad subsystem categories.
- **Keep local:** addon zip archives, extracted addon source, and exploratory
  scratch scans under `artifacts/` or another gitignored location.

## Files

| Report | Description |
| --- | --- |
| `addon-corpus-summary.md` | Human-readable corpus summary and top API/event tables |
| `addon_api_summary.json` | Machine-readable scan metrics and category counts |
| `addon_apollo_api_scan.json` | API usage grouped by client namespace |
| `addon_category_map.tsv` | Addon-to-category map |
| `addon_event_frequency.tsv` | Apollo event frequency by addon count |
| `addon_inventory.txt` | Scanned addon archive names |

## Refresh

```powershell
python Tools\AddonCorpusAudit\audit_addon_corpus.py `
  --addon-dir "I:\Wildstar Addons" `
  --output-dir Tools\AddonCorpusAudit\reports
```
