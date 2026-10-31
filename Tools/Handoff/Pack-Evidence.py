"""Freeze reviewed loose gzip objects into bounded Git-friendly pack files.

This only packages already sanitized bytes; run credential review first.
"""
from pathlib import Path
import gzip, json, re
from evidence_store import digest, file_digest

store = Path(__file__).resolve().parents[2]/'Evidence/Continuation'
summary = json.loads((store/'summary.json').read_text())
if summary['Format'] != 1: raise SystemExit('Only a reviewed, unpacked snapshot can be packed')
review = json.loads((store/'review.json').read_text())
if review['CredentialSignatureIssues'] != 0 or review['ReviewedMembers'] != summary['Members'] or review['IndexSHA256'] != summary['IndexSHA256'] or review['ExclusionsSHA256'] != summary['ExclusionsSHA256']:
    raise SystemExit('A successful full credential review is required')
if (store/'packs').exists(): raise SystemExit('Refusing to overwrite existing packs')
with gzip.open(store/'index.jsonl.gz', 'rt', encoding='utf-8') as stream:
    rows = [json.loads(line) for line in stream]
keys = sorted({chunk['SHA256'] for row in rows for chunk in row['Chunks']})
if any(not re.fullmatch('[0-9a-f]{64}', key) for key in keys): raise ValueError('Invalid object hash')
(store/'packs').mkdir()
index = {}; packs = []; output = None; length = 0; name = None
try:
    for key in keys:
        data = (store/'objects'/key[:2]/(key+'.gz')).read_bytes()
        if digest(gzip.decompress(data)) != key: raise ValueError('Invalid source object')
        if output is None or length + len(data) > 48*1024*1024:
            if output:
                output.close(); packs.append({'File': name, 'Bytes': length, 'SHA256': file_digest(store/'packs'/name)})
            name = 'objects-'+str(len(packs)).zfill(3)+'.pack'
            output = (store/'packs'/name).open('xb'); length = 0
        index[key] = {'Pack': name, 'Offset': length, 'Bytes': len(data)}
        output.write(data); length += len(data)
    if output:
        output.close(); output = None
        packs.append({'File': name, 'Bytes': length, 'SHA256': file_digest(store/'packs'/name)})
finally:
    if output: output.close()
(store/'object-index.json.gz').write_bytes(gzip.compress(json.dumps(index, separators=(',', ':')).encode(), compresslevel=6, mtime=0))
summary.update({'Format': 2, 'ObjectIndexSHA256': file_digest(store/'object-index.json.gz'), 'Packs': packs})
(store/'summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
# Delete only verified, generated loose objects now retained in packs.
for key in keys: (store/'objects'/key[:2]/(key+'.gz')).unlink()
for folder in (store/'objects').iterdir():
    if re.fullmatch('[0-9a-f]{2}', folder.name): folder.rmdir()
(store/'objects').rmdir()
print(json.dumps({'PackedObjects': len(keys), 'Packs': len(packs), 'StoredBytes': sum(p['Bytes'] for p in packs)}))
