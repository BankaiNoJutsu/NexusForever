# Wiki Archive Audit Reports

Committed snapshots from `audit_wildstar_wiki.py` and related offline audits.

## Policy

- **Track here:** domain audit markdown that summarizes wiki vs client-table vs server
  implementation gaps and is useful for planning and PR review.
- **Keep under `artifacts/` (gitignored):** large inputs and scratch output
  - `artifacts/wildstar-fandom-wiki-*` — full Fandom wiki dump
  - `artifacts/wiki-audit/` — local reruns before promoting a snapshot here
  - `artifacts/load-tests/` — login/world profiling JSON
  - `artifacts/verify-obj/`, `artifacts/review-bin/` — build intermediates
  - `artifacts/verify/*-evidence/` — spell, loot, proc capture JSON from live sessions

## Files

| Report | Generator | Last refreshed |
| --- | --- | --- |
| `abilities-wiki-audit.md` | `audit_wildstar_wiki.py --domain abilities` | 2026-06-06 |
| `achievements-wiki-audit.md` | `audit_wildstar_wiki.py --domain achievements` | 2026-06-06 |
| `amps-wiki-audit.md` | `audit_wildstar_wiki.py --domain amps` | 2026-06-06 |
| `character-creation-wiki-audit.md` | `audit_wildstar_wiki.py --domain character-creation` | 2026-06-06 |
| `housing-wiki-audit.md` | `audit_wildstar_wiki.py --domain housing` | 2026-06-06 |
| `lore-wiki-audit.md` | `audit_wildstar_wiki.py --domain lore` | 2026-06-06 |
| `quests-wiki-audit.md` | `audit_wildstar_wiki.py --domain quests` | 2026-05-23 |
| `tradeskills-wiki-audit.md` | `audit_wildstar_wiki.py --domain tradeskills` | 2026-06-06 |

Some report refreshes intentionally exit nonzero when the audited domain still
has hard gaps. Keep those snapshots when the markdown report was written; the
`Result:` line records whether the domain passed or failed.

Quest bucket coverage (curated vs generic vs blocked) is maintained separately:

- `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md` — human summary
- `Decomp/Analysis/coverage/QUEST_IMPLEMENTATION_AUDIT.md` — generated inventory
- `Tools/WikiArchiveAudit/quest_implementation_audit.py` — generator

## Refresh a report

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain quests `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output Tools\WikiArchiveAudit\reports\quests-wiki-audit.md
```

Other domains can be promoted the same way (`character-creation`, `abilities`,
`amps`, `housing`, `achievements`, `tradeskills`, `lore`) when a pass needs a
new committed snapshot.
