import csv, io, sys
sys.stdout.reconfigure(encoding='utf-8')
labels_path = r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\function_labels.csv'
ev_path = r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\mapping_artifacts\function_evidence_inventory.csv'

# label rows by (program, address): line number (as the updater counts: every physical line), name, comment
rows = {}
with open(labels_path, encoding='utf-8') as f:
    for n, line in enumerate(f, 1):
        t = line.strip()
        if not t or t.startswith('#') or t.startswith('program,'):
            continue
        parts = t.split(',', 3)
        if len(parts) < 3:
            continue
        key = (parts[0].strip().lower(), parts[1].strip().lower())
        rows[key] = (n, parts[2].strip(), parts[3].strip() if len(parts) > 3 else '')

raw = open(ev_path, 'rb').read()
nl = b'\r\n' if b'\r\n' in raw else b'\n'
text = raw.decode('utf-8')
reader = csv.reader(io.StringIO(text, newline=''))
header = next(reader)
idx = {h: i for i, h in enumerate(header)}
out = [header]
changed_name = changed_row = missing = 0
for r in reader:
    if len(r) < len(header):
        r += [''] * (len(header) - len(r))
    key = (r[idx['binary']].lower(), r[idx['address']].lower())
    if key in rows:
        n, name, comment = rows[key]
        if r[idx['durableLabel']] != name:
            r[idx['durableLabel']] = name
            if comment:
                r[idx['purpose']] = comment
                r[idx['anchors']] = comment
            changed_name += 1
        if r[idx['labelRow']] != str(n):
            r[idx['labelRow']] = str(n)
            changed_row += 1
    else:
        missing += 1
    out.append(r)
buf = io.StringIO()
w = csv.writer(buf, quoting=csv.QUOTE_ALL, lineterminator=nl.decode())
w.writerows(out)
open(ev_path, 'wb').write(buf.getvalue().encode('utf-8'))
print('rows', len(out) - 1, 'durableLabel updated', changed_name, 'labelRow updated', changed_row, 'no label row', missing)
