"""Build the explicitly authorized public, text-only continuation snapshot.

Run only on the source machine. Never imports private runtime directories.
The allowlist, exclusions and redactions are part of the published provenance.
"""
from pathlib import Path
from collections import Counter
import argparse, gzip, json, os, re
from evidence_store import digest, sanitize_json, sanitize_json_lines, sanitize_text, TOKEN

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--assessment-report', type=Path, required=True)
parser.add_argument('--extend', action='store_true', help='Add newly allowlisted text to an existing snapshot; final review is still required.')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]
store = repo / 'Evidence/Continuation'
if store.exists() and not args.extend: raise SystemExit('Refusing to overwrite an existing snapshot')
store.mkdir(parents=True, exist_ok=True)
old_rows = []
old_summary = {}
if args.extend:
    old_summary = json.loads((store/'summary.json').read_text())
    if old_summary['Format'] != 1: raise SystemExit('Packed snapshots are frozen; rebuild from source before changing membership')
    with gzip.open(store/'index.jsonl.gz', 'rt', encoding='utf-8') as stream: old_rows = [json.loads(line) for line in stream]
published = {row['SourcePath'] for row in old_rows}
extensions = {'.json', '.jsonl', '.csv', '.md', '.txt', '.log', '.trx', '.cs', '.csproj', '.py', '.ps1', '.lua', '.c', '.properties', '.sql', '.srt', '.vtt', '.java', '.xml', '.tsv', '.yaml', '.yml', '.toml', '.patch', '.diff', '.props', '.targets', '.slnx', '.manifest', '.prepared', '.h', '.html', '.sh', '.dotsettings'}
prune = {'.git', 'bin', 'obj', '__pycache__', 'node_modules', 'frozen', 'frozen-native', 'broker-package', 'broker-state', 'database-copies', 'runtime', 'runtime-runs', 'runtime-assets', 'full-prepared-assets'}
private_names = {'start-isolatedbroker.ps1', 'invoke-isolatedbroker.ps1', 'stsserver.json', 'authserver.json', 'worldserver.json', 'chatserver.json', 'storefrontserver.json', 'appsettings.json', 'appsettings.development.json'}
private_path = re.compile(r'(?i)(?:^|/)(?:[^/]*\.)?(?:stdout|stderr)\.log$|(?:^|/)(?:client|server|auth|sts|world|chat|broker)[^/]*\.log$|(?:credentials|transfer-key|private-key|mysql[-_]defaults|my\.cnf|my\.ini)')
roots = [('assessment-report', 'assessment', args.assessment_report),
         ('decomp-exports', 'reverse-engineering', repo/'Decomp/Analysis/exports'),
         ('decomp-logs', 'reverse-engineering', repo/'Decomp/Analysis/logs'),
         ('data-mapping/output', 'data-mapping', repo/'Tools/DataMapping/output'),
         ('data-mapping/review', 'data-mapping', repo/'Tools/DataMapping/review'),
         ('reference-sql/jabbithole_mysql', 'reference-sql', repo/'jabbithole_mysql'),
         ('reference-sql/wildstar_client_mysql', 'reference-sql', repo/'wildstar_client_mysql')]
for name in ['continue-nexus-decompile', 'nexus-change-review', 'nexus-data-mapping', 'nexus-load-test', 'nexus-local-runtime', 'nexus-row-closure']:
    roots.append(('agent-skills/'+name, 'agent-skills', Path.home()/'.codex/skills'/name))
roots.append(('agent-skills/reverse-engineer-anything', 'agent-skills', Path.home()/'.agents/skills/reverse-engineer-anything'))
counts = Counter(row['Category'] for row in old_rows); excluded = Counter()
redacted = sum(bool(row['Redactions']) for row in old_rows)
source_bytes = old_summary.get('SourceBytes', 0); published_bytes = sum(row['PublishedBytes'] for row in old_rows)
objects = {chunk['SHA256'] for row in old_rows for chunk in row['Chunks']}
def exclude(path, reason):
    excluded[reason] += 1
    exclusions.write(json.dumps({'SourcePath': str(path), 'Reason': reason})+'\n')

with gzip.GzipFile(filename=str(store/'index.jsonl.gz'), mode='wb', mtime=0) as compressed_index, gzip.open(store/'exclusions.jsonl.gz', 'wt', encoding='utf-8') as exclusions:
    for row in old_rows: compressed_index.write((json.dumps(row, ensure_ascii=False)+'\n').encode('utf-8'))
    for prefix, category, root in roots:
        if not root.is_dir(): exclude(root, 'source-directory-unavailable'); continue
        for folder, dirs, files in os.walk(root):
            for name in sorted(dirs[:]):
                if name.lower() in prune or name.lower().startswith('runtime-checkpoint'):
                    exclude(Path(folder)/name, 'private-runtime/build/binary-directory (entire subtree)'); dirs.remove(name)
            dirs.sort()
            for name in sorted(files):
                path = Path(folder)/name; relative = path.relative_to(root).as_posix()
                if str(path) in published: continue
                if path.is_symlink(): exclude(path, 'symbolic-link'); continue
                if name.lower() in private_names or private_path.search(relative): exclude(path, 'private-runtime/configuration/log'); continue
                if path.suffix.lower() not in extensions: exclude(path, 'binary/media/cache-or-unselected-extension'); continue
                if path.suffix.lower() == '.log' and not re.search(r'(?i)(build|test|verif|restore|export|ghidra|coverage|proof|review|baseline|focused|compile)', name):
                    exclude(path, 'unselected-live-log'); continue
                # Reference SQL and explicitly reviewed probes only; private dumps stay local.
                reviewed_sql = {'remaining-goal/migration-preservation-check.sql', 'taxi-mount/mysql-taxi-migration.sql', 'travel-archive/mysql-bind-migration.sql'}
                if path.suffix.lower() == '.sql' and category not in ['reference-sql', 'agent-skills'] and relative not in reviewed_sql:
                    exclude(path, 'potential-runtime-database-SQL'); continue
                data = path.read_bytes(); original = digest(data); changes = []
                try: text = data.decode('utf-8-sig')
                except UnicodeDecodeError: exclude(path, 'non-UTF8-or-binary'); continue
                if '\0' in text: exclude(path, 'binary-NUL'); continue
                if TOKEN.search(text): exclude(path, 'credential-token-or-private-key-signature'); continue
                if path.suffix.lower() == '.json':
                    try: data, changes = sanitize_json(data); text = data.decode('utf-8-sig')
                    except (ValueError, UnicodeDecodeError): exclude(path, 'invalid-JSON'); continue
                elif path.suffix.lower() == '.jsonl':
                    data, changes = sanitize_json_lines(data)
                data, text_changes = sanitize_text(data); changes.extend(text_changes)
                sha = digest(data); chunks = []
                for start in range(0, len(data), 8*1024*1024):
                    part = data[start:start+8*1024*1024]; key = digest(part)
                    obj = store/'objects'/key[:2]/(key+'.gz')
                    if key not in objects:
                        obj.parent.mkdir(parents=True, exist_ok=True); obj.write_bytes(gzip.compress(part, compresslevel=6, mtime=0)); objects.add(key)
                    chunks.append({'SHA256': key, 'Bytes': len(part)})
                row = {'Path': prefix+'/'+relative, 'Category': category, 'SourcePath': str(path), 'SourceSHA256': original, 'PublishedSHA256': sha, 'PublishedBytes': len(data), 'Redactions': changes, 'Chunks': chunks}
                compressed_index.write((json.dumps(row, ensure_ascii=False)+'\n').encode('utf-8'))
                counts[category] += 1; redacted += bool(changes); source_bytes += path.stat().st_size; published_bytes += len(data)
                if sum(counts.values()) % 5000 == 0: print(json.dumps({'Published': sum(counts.values()), 'Category': category}), flush=True)
summary = {'Format': 1, 'Date': '2026-10-31', 'ImplementationCommit': '71cde855988446c304341f906556542d6868c412', 'Categories': dict(counts), 'Members': sum(counts.values()), 'SourceBytes': source_bytes, 'PublishedBytes': published_bytes, 'UniqueObjects': len(objects), 'RedactedMembers': redacted, 'Exclusions': dict(excluded), 'Roots': [{'Prefix': p, 'Category': c, 'HistoricalPath': str(r)} for p,c,r in roots], 'IndexSHA256': digest((store/'index.jsonl.gz').read_bytes()), 'ExclusionsSHA256': digest((store/'exclusions.jsonl.gz').read_bytes()), 'Qualification': 'Historical evidence and sanitized derivatives; not a deployment or fresh provider/native qualification.'}
(store/'summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k not in ['Roots']}, indent=2))
