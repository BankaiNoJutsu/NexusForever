from pathlib import Path
import argparse, gzip, json
from evidence_store import restore, read_chunks, file_digest, safe_target

parser = argparse.ArgumentParser(description='Restore or verify sanitized, published continuation evidence.')
parser.add_argument('--output', type=Path)
parser.add_argument('--category', action='append', choices=['assessment', 'reverse-engineering', 'data-mapping', 'reference-sql', 'agent-skills'], help='Restore selected category; repeat as needed.')
parser.add_argument('--verify-only', action='store_true')
parser.add_argument('--legacy-path', help='Locate a historical absolute source-machine path in the store.')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]; store = repo / 'Evidence/Continuation'
output = (args.output or repo / '.continuation-evidence').resolve()
summary = json.loads((store / 'summary.json').read_text())
if summary['Format'] not in (1, 2): raise ValueError('Unsupported evidence store format')
for name, key in [('index.jsonl.gz', 'IndexSHA256'), ('exclusions.jsonl.gz', 'ExclusionsSHA256')]:
    if file_digest(store / name) != summary[key]: raise ValueError('Snapshot manifest failed its digest: ' + name)
if summary['Format'] == 2:
    if file_digest(store/'object-index.json.gz') != summary['ObjectIndexSHA256']: raise ValueError('Object index failed its digest')
    for pack in summary['Packs']:
        import re
        if not re.fullmatch(r'objects-\d{3}\.pack', pack['File']): raise ValueError('Invalid pack name')
        path = store/'packs'/pack['File']
        if path.stat().st_size != pack['Bytes'] or file_digest(path) != pack['SHA256']: raise ValueError('Evidence pack failed its digest')
count = 0
with gzip.open(store / 'index.jsonl.gz', 'rt', encoding='utf-8') as stream:
    for line in stream:
        row = json.loads(line)
        safe_target(output, row['Path'])
        if args.legacy_path:
            if row['SourcePath'].replace('\\', '/').lower() != args.legacy_path.replace('\\', '/').lower(): continue
            print(json.dumps({'Member': row['Path'], 'RestoredPath': str(output / row['Path']), 'Redactions': row['Redactions']}, indent=2))
            count += 1; continue
        if args.category and row['Category'] not in args.category: continue
        if args.verify_only:
            import hashlib
            h = hashlib.sha256(); size = 0
            for data in read_chunks(store, row): h.update(data); size += len(data)
            if h.hexdigest() != row['PublishedSHA256'] or size != row['PublishedBytes']: raise ValueError(row['Path'])
        else:
            restore(store, row, output)
        count += 1
if args.legacy_path and not count: raise SystemExit('No published member for that historical path; consult OPTIONAL_DATASETS.md and exclusions.jsonl.gz.')
if not args.category and not args.legacy_path and count != summary['Members']: raise ValueError('Snapshot member count mismatch')
print(('Located' if args.legacy_path else 'Verified' if args.verify_only else 'Restored') + f' {count} evidence members.')
print('Redacted records are published derivatives. Original hashes/history do not make them byte-identical originals or new live verification.')
