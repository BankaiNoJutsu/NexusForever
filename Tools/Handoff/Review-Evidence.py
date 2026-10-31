"""Final credential review of the public text store, before staging any objects.

Rechecks structured auth arrays and line-oriented JSON, and emits audit counts.
Never emits credential values. Run --sanitize to apply conservative derivatives.
"""
from pathlib import Path
import argparse, gzip, json, os, re
from evidence_store import digest, read_chunks, sanitize_json, sanitize_json_lines, sanitize_text, TOKEN, SECRET_KEY

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--sanitize', action='store_true')
parser.add_argument('--private-config-root', type=Path, help='Compare against parsed local server-config secrets without publishing their values.')
args = parser.parse_args()
store = Path(__file__).resolve().parents[2]/'Evidence/Continuation'
summary = json.loads((store/'summary.json').read_text())
if args.sanitize and summary['Format'] != 1: raise SystemExit('Packed snapshots are frozen; review/rebuild loose objects before packing')
rows = []
with gzip.open(store/'index.jsonl.gz', 'rt', encoding='utf-8') as source:
    rows = [json.loads(line) for line in source]
changed = 0; issues = []
secrets = set()
if args.private_config_root:
    def collect(value):
        if not isinstance(value, dict): return
        for key, item in value.items():
            if isinstance(item, dict): collect(item)
            elif isinstance(item, str) and SECRET_KEY.search(key):
                if 'connectionstring' in key.lower():
                    for match in re.finditer(r'(?i)(?:password|pwd)\s*=\s*([^;]+)', item):
                        if len(match[1].strip()) >= 5: secrets.add(match[1].strip())
                elif len(item) >= 5 and item != '[REDACTED]' and key.lower() not in ['hash', 'salt', 'verifier']:
                    secrets.add(item)
    for folder, dirs, files in os.walk(args.private_config_root):
        dirs[:] = [d for d in dirs if d.lower() not in {'.git', 'node_modules', 'database-copies', 'broker-state', 'broker-package', 'frozen', 'frozen-native'}]
        for name in files:
            if not re.fullmatch(r'(?i)(?:[a-z]*server(?:\.[a-z]+)*|appsettings(?:\.[a-z]+)*)\.json', name): continue
            try: collect(json.loads((Path(folder)/name).read_text(encoding='utf-8-sig')))
            except (ValueError, UnicodeDecodeError): pass
for row in rows:
    data = b''.join(read_chunks(store, row))
    if digest(data) != row['PublishedSHA256']: raise ValueError('File digest mismatch: '+row['Path'])
    if TOKEN.search(data.decode('utf-8-sig')): issues.append((row['Path'], 'token/private-key-signature'))
    clean = data; fields = []
    if row['Path'].lower().endswith('.json'):
        clean, fields = sanitize_json(data)
    elif row['Path'].lower().endswith('.jsonl'):
        clean, fields = sanitize_json_lines(data)
    clean, text_fields = sanitize_text(clean); fields.extend(text_fields)
    text = clean.decode('utf-8-sig')
    for secret in sorted(secrets, key=len, reverse=True):
        if secret in text:
            text = text.replace(secret, '[REDACTED]'); fields.append('known-local-config-credential')
    if fields and text != clean.decode('utf-8-sig'): clean = text.encode('utf-8')
    # A repeated redaction placeholder is already safe, not another derivative.
    if clean == data: fields = []
    if fields:
        changed += 1
        if not args.sanitize:
            issues.append((row['Path'], 'additional-structured-credential-redaction')); continue
        row['Redactions'].extend('final-review:'+field for field in fields)
        row['PublishedSHA256'] = digest(clean); row['PublishedBytes'] = len(clean); row['Chunks'] = []
        for start in range(0, len(clean), 8*1024*1024):
            data_chunk = clean[start:start+8*1024*1024]; sha = digest(data_chunk)
            obj = store/'objects'/sha[:2]/(sha+'.gz')
            obj.parent.mkdir(parents=True, exist_ok=True)
            if not obj.exists(): obj.write_bytes(gzip.compress(data_chunk, compresslevel=6, mtime=0))
            row['Chunks'].append({'SHA256': sha, 'Bytes': len(data_chunk)})
if issues:
    print(json.dumps({'Issues': issues[:100], 'Count': len(issues)}, indent=2))
    raise SystemExit(1)
if args.sanitize:
    with gzip.GzipFile(filename=str(store/'index.jsonl.gz'), mode='wb', mtime=0) as target:
        for row in rows: target.write((json.dumps(row, ensure_ascii=False)+'\n').encode('utf-8'))
    retained = {c['SHA256'] for row in rows for c in row['Chunks']}
    # Delete only obsolete, generated hash objects inside this exact store.
    for obj in (store/'objects').glob('*/*.gz'):
        if re.fullmatch('[0-9a-f]{64}', obj.stem) and obj.stem not in retained:
            obj.unlink()
    summary.update({'UniqueObjects': len(retained), 'RedactedMembers': sum(bool(r['Redactions']) for r in rows), 'PublishedBytes': sum(r['PublishedBytes'] for r in rows), 'IndexSHA256': digest((store/'index.jsonl.gz').read_bytes())})
    (store/'summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'ReviewedMembers': len(rows), 'AdditionalSanitizedMembers': changed, 'CredentialSignatureIssues': len(issues), 'ComparedLocalCredentialValues': len(secrets), 'IndexSHA256': summary['IndexSHA256'], 'ExclusionsSHA256': summary['ExclusionsSHA256']}))
