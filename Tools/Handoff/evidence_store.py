"""Small, verifiable text-evidence store. No credentials or runtime execution."""
from pathlib import Path, PurePosixPath
import gzip, hashlib, json, re
from functools import lru_cache

SECRET_KEY = re.compile(r'(?i)(password|passwd|pwd|connectionstring|privatekey|clientsecret|apikey|access.?token|refresh.?token|session.?key|shared.?key|keydata|verifier|authorization|cookie|^salt$|^hash$)')
TOKEN = re.compile(r'(?:gh[pousr]_[A-Za-z0-9]{25,}|github_pat_[A-Za-z0-9_]{30,}|AKIA[0-9A-Z]{16}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----)')

def lean_member(row):
    name = row['Path']; parts = name.split('/'); suffix = Path(name).suffix.lower()
    bulk = {'selected_call_edges.csv', 'call_edges.csv', 'functions.csv', 'imports.csv', 'strings.csv', 'string_xrefs.csv', 'interesting_strings.csv'}
    if Path(name).name in bulk: return False
    if row['Category'] == 'agent-skills': return True
    if row['Category'] != 'assessment': return False
    if len(parts) == 2: return suffix in {'.md', '.json', '.csv', '.txt'}
    if parts[1] == 'remaining-goal':
        if len(parts) == 3: return suffix in {'.md', '.json', '.csv'}
        if parts[2] == 'realm-bank-splits': return suffix not in {'.log', '.trx'}
        if parts[2] not in {'native-reconciliation-tests', 'recovered-cache', 'recovered-ghidra'} and any(word in parts[2] for word in ['native', 'exports', 'labeled-export', 'reset-evidence']):
            return suffix not in {'.log', '.trx'}
    return False

def digest(data):
    return hashlib.sha256(data).hexdigest()

def sanitize_json(data):
    changed = []
    def visit(value, path=''):
        if isinstance(value, dict):
            result = {}
            for key, item in value.items():
                name = path + '/' + key
                if SECRET_KEY.search(key) and not isinstance(item, (dict, bool)) and item not in (None, '', [], 0):
                    result[key] = '[REDACTED]'; changed.append(name)
                elif key.lower() in ['email', 'emailaddress'] and isinstance(item, str) and item and not item.lower().endswith(('@example.local', '@example.com', '@test.local', '@example.invalid')):
                    result[key] = '[REDACTED]'; changed.append(name)
                else:
                    result[key] = visit(item, name)
            return result
        if isinstance(value, list):
            return [visit(item, path + '/' + str(i)) for i, item in enumerate(value)]
        return value
    parsed = json.loads(data.decode('utf-8-sig'))
    cleaned = visit(parsed)
    return (json.dumps(cleaned, ensure_ascii=False, indent=2).encode('utf-8') if changed else data), changed

def sanitize_text(data):
    text = data.decode('utf-8-sig'); original = text; changed = []; lower = text.lower()
    patterns = [
        ('URI-user-password', r'(?i)\b((?:amqps?|mysql|postgres(?:ql)?|https?)://)[^/\s:@]+:[^/@\s]+@', lambda m: m[1]+'[REDACTED]@'),
        ('literal-credential-assignment', r'''(?i)(["']?(?:password(?:hash)?|passwd|pwd|connectionstring|privatekey|clientsecret|apikey|access[_-]?token|refresh[_-]?token|session[_-]?key|salt|verifier)["']?\s*[:=]\s*)(["'])([^\r\n]{0,8192}?)\2''', lambda m: m[1]+m[2]+'[REDACTED]'+m[2]),
        ('connection-password', r'''(?i)(\b(?:password|pwd)\s*=\s*)([^;\s"'<>]+)''', lambda m: m[1]+'[REDACTED]'),
        ('command-password', r'''(?i)(--password(?:=|\s+))([^\s"'<>]+)''', lambda m: m[1]+'[REDACTED]'),
    ]
    for name, pattern, replace in patterns:
        if name == 'URI-user-password' and '://' not in text: continue
        if name == 'literal-credential-assignment' and not any(word in lower for word in ['password', 'passwd', 'pwd', 'connectionstring', 'privatekey', 'clientsecret', 'apikey', 'token', 'session', 'salt', 'verifier']): continue
        if name == 'connection-password' and 'password' not in lower and 'pwd' not in lower: continue
        if name == 'command-password' and '--password' not in lower: continue
        text, count = re.subn(pattern, replace, text)
        if count: changed.append(name+':'+str(count))
    domains = {'example.local', 'example.com', 'example.invalid', 'test.local', 'test.com', 'localhost.local', 'nexusforever.dev'}
    def clean_email(match):
        return match[0] if match[0].lower().split('@')[1] in domains else '[REDACTED-EMAIL]'
    text, count = re.subn(r'(?i)\b[A-Z0-9._%+-]{1,64}@[A-Z0-9.-]{1,253}\.[A-Z]{2,63}\b', clean_email, text) if '@' in text else (text, 0)
    if count and text != original: changed.append('email-review')
    return (text.encode('utf-8'), changed) if text != original else (data, [])

def sanitize_json_lines(data):
    lines = []; changed = []
    for number, line in enumerate(data.splitlines(keepends=True), 1):
        try: part, fields = sanitize_json(line)
        except ValueError: part, fields = line, []
        if fields:
            part = json.dumps(json.loads(part), ensure_ascii=False).encode('utf-8') + b'\n'
            changed.extend(str(number)+field for field in fields)
        lines.append(part)
    result = b''.join(lines)
    return result, changed if result != data else []

def safe_target(root, name):
    path = PurePosixPath(name)
    if path.is_absolute() or '..' in path.parts or '\\' in name or ':' in name:
        raise ValueError('Unsafe evidence member path')
    target = (root / Path(*path.parts)).resolve()
    if not target.is_relative_to(root.resolve()):
        raise ValueError('Evidence member escapes output root')
    return target

@lru_cache(maxsize=4)
def packed_index(store):
    return json.loads(gzip.decompress((store/'object-index.json.gz').read_bytes()))

def object_bytes(store, sha):
    loose = store/'objects'/sha[:2]/(sha+'.gz')
    if loose.is_file(): return loose.read_bytes()
    entry = packed_index(store)[sha]
    if not re.fullmatch(r'objects-\d{3}\.pack', entry['Pack']) or entry['Offset'] < 0 or entry['Bytes'] < 0:
        raise ValueError('Invalid packed object location')
    with (store/'packs'/entry['Pack']).open('rb') as stream:
        stream.seek(entry['Offset']); data = stream.read(entry['Bytes'])
    if len(data) != entry['Bytes']: raise ValueError('Truncated packed object')
    return data

def read_chunks(store, row):
    for chunk in row['Chunks']:
        sha = chunk['SHA256']
        if not re.fullmatch('[0-9a-f]{64}', sha):
            raise ValueError('Invalid object hash')
        data = gzip.decompress(object_bytes(store, sha))
        if len(data) != chunk['Bytes'] or digest(data) != sha:
            raise ValueError('Evidence object failed its digest')
        yield data

def restore(store, row, output):
    target = safe_target(output, row['Path'])
    if target.exists():
        if not target.is_file() or file_digest(target) != row['PublishedSHA256']:
            raise ValueError('Refusing to replace different existing evidence: ' + row['Path'])
        return
    target.parent.mkdir(parents=True, exist_ok=True)
    # Recheck after making parents in case an existing link redirects the path.
    target = safe_target(output, row['Path'])
    temporary = target.with_name(target.name + '.handoff-part')
    if temporary.exists():
        raise ValueError('A partial restore already exists')
    h = hashlib.sha256(); count = 0
    try:
        with temporary.open('xb') as out:
            for data in read_chunks(store, row):
                out.write(data); h.update(data); count += len(data)
        if h.hexdigest() != row['PublishedSHA256'] or count != row['PublishedBytes']:
            raise ValueError('Evidence member failed its complete digest')
        temporary.replace(target)
    finally:
        if temporary.exists(): temporary.unlink()

def file_digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for data in iter(lambda: stream.read(8 * 1024 * 1024), b''): h.update(data)
    return h.hexdigest()
