"""Apply the owner's lean handoff preference to the local text-only draft.

The full draft is preserved in ignored .continuation-evidence/full-draft.
Original external inputs are never moved or deleted.
"""
from pathlib import Path
from collections import Counter
import gzip, json, shutil
from evidence_store import digest, lean_member, file_digest

repo = Path(__file__).resolve().parents[2]
store = repo/'Evidence/Continuation'
backup = repo/'.continuation-evidence/full-draft'
summary = json.loads((store/'summary.json').read_text())
if summary['Format'] != 1 or file_digest(store/'index.jsonl.gz') != summary['IndexSHA256']:
    raise SystemExit('An intact loose-object draft is required')
if backup.exists(): raise SystemExit('Refusing to replace the retained full draft')
with gzip.open(store/'index.jsonl.gz', 'rt', encoding='utf-8') as stream: rows = [json.loads(line) for line in stream]
selected = [dict(row) for row in rows if lean_member(row)]
for row in selected:
    parts = row['Path'].split('/')
    if len(parts) > 3 and parts[1] == 'remaining-goal' and parts[2] != 'realm-bank-splits': row['Category'] = 'reverse-engineering'
keys = {chunk['SHA256'] for row in selected for chunk in row['Chunks']}
if not selected: raise SystemExit('Lean selection cannot be empty')
backup.parent.mkdir(parents=True, exist_ok=True)
# Verify both resolved move endpoints remain in the explicitly named workspace.
if not store.resolve().is_relative_to(repo.resolve()) or not backup.resolve().is_relative_to(repo.resolve()) or not backup.resolve().is_relative_to((repo/'.continuation-evidence').resolve()):
    raise ValueError('Unexpected publication move target')
store.rename(backup)
store.mkdir(parents=True)
for name in ['README.md', 'exclusions.jsonl.gz']:
    shutil.copyfile(backup/name, store/name)
with gzip.GzipFile(filename=str(store/'index.jsonl.gz'), mode='wb', mtime=0) as stream:
    for row in selected: stream.write((json.dumps(row, ensure_ascii=False)+'\n').encode())
for key in keys:
    target = store/'objects'/key[:2]/(key+'.gz'); target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(backup/'objects'/key[:2]/(key+'.gz'), target)
omitted = Counter(row['Category'] for row in rows if not lean_member(row))
optional = {'Scope': 'Owner-selected lean handoff, 2026-10-31', 'OmittedMembers': len(rows)-len(selected), 'OmittedCategories': dict(omitted), 'OptionalSources': summary['Roots'], 'OriginalInputsPreserved': True, 'Notes': 'Bulk history, general export caches, static reference SQL and generated mapping CSVs are optional and are not published in this lean store. See OPTIONAL_DATASETS.md.'}
(store/'optional-datasets.json').write_text(json.dumps(optional, indent=2)+'\n', encoding='utf-8')
summary.update({'Selection': 'lean', 'Members': len(selected), 'Categories': dict(Counter(row['Category'] for row in selected)), 'SourceBytes': sum(Path(row['SourcePath']).stat().st_size for row in selected), 'PublishedBytes': sum(row['PublishedBytes'] for row in selected), 'UniqueObjects': len(keys), 'RedactedMembers': sum(bool(row['Redactions']) for row in selected), 'IndexSHA256': file_digest(store/'index.jsonl.gz')})
(store/'summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'Members': len(selected), 'ExpandedBytes': summary['PublishedBytes'], 'CompressedObjectBytes': sum((store/'objects'/key[:2]/(key+'.gz')).stat().st_size for key in keys)}))
