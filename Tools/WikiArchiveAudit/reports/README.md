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
| `quests-wiki-audit.md` | `audit_wildstar_wiki.py --domain quests` | 2026-05-23 |

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
`housing`, `achievements`, `tradeskills`, `lore`) when a pass needs a new
committed snapshot.
