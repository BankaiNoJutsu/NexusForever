p = r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\function_labels.csv'
raw = open(p, 'rb').read()
replace = {a.lower() for a in open(r'I:\tools\string_labels_replace.txt').read().split() if a}
gen = open(r'I:\tools\string_labels_generated.csv', 'rb').read()
hdr = b'# String-anchor pass (2026-11-08): WildStar64.exe functions named from the strings they reference (ClientDB_Register<Table> template functions and callers of ClientEvent_DispatchNamedEvent); see LABEL_COVERAGE_PASS_STRING_ANCHORS_20261108.md. Replaces earlier FUN_ placeholder rows at the same addresses.\r\n'
if hdr in raw:
    raise SystemExit('already appended')
kept = []
removed = 0
for line in raw.split(b'\r\n'):
    parts = line.split(b',', 3)
    if len(parts) >= 3 and parts[0] == b'WildStar64.exe' and parts[1].decode().lower() in replace and parts[2].startswith(b'FUN_'):
        removed += 1
        continue
    kept.append(line)
body = b'\r\n'.join(kept)
if not body.endswith(b'\r\n'):
    body += b'\r\n'
open(p, 'wb').write(body + hdr + gen)
print('removed placeholders', removed, 'appended', gen.count(b'\r\n'))
