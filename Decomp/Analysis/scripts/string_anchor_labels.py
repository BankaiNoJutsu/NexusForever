import re, csv, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
LABELS = r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\function_labels.csv'

existing = set()
placeholder = set()
for row in csv.reader(open(LABELS, encoding='utf-8')):
    if row and not row[0].startswith('#') and row[0] != 'program' and row[0] == 'WildStar64.exe':
        (placeholder if row[2].startswith('FUN_') else existing).add(row[1].lower())

def clean(s):
    return re.sub(r'[^A-Za-z0-9]+', '_', s).strip('_')

out = {}  # addr -> (name, comment, rule)

# rule 1: ClientDB registration template (size 515, one DB\<Table>.tbl string)
pat = re.compile(r'^DB\\+([A-Za-z0-9_]+)\.tbl$')
for l in open(r'I:\tools\unique_strings.tsv', encoding='utf-8'):
    r = l.rstrip('\n').split('\t')
    addr, size, strs = r[0], int(r[1]), r[2:]
    for s in strs:
        m = pat.match(s)
        if m:
            others = [x for x in strs if x != s]
            extra = '' if others == [m.group(1)] else ' (descriptor strings: %s)' % ' / '.join(clean(x) for x in others)
            out[addr] = ('ClientDB_Register' + m.group(1),
                         'Registers the %s client DB table from DB/%s.tbl; one of 362 identical %d-byte registration functions%s; automatic string-anchor pass.' % (m.group(1), m.group(1), size, extra),
                         'db-table')
            break

# rule 2: functions that raise Lua events through ClientEvent_DispatchNamedEvent
by = collections.defaultdict(lambda: {'size': 0, 'fn': '', 'names': []})
helper = ''
for l in open(r'I:\tools\event_callers.tsv', encoding='utf-8'):
    if l.startswith('#'):
        helper = l.split()[-1]
        continue
    p = l.rstrip('\n').split('\t')
    d = by[p[0]]
    d['size'] = int(p[2]); d['fn'] = p[1]
    d['names'].append(p[4] if len(p) > 4 else '')
cand = {}
for addr, d in by.items():
    if not d['fn'].startswith('FUN_') or addr in out:
        continue
    names = sorted({n for n in d['names'] if n})
    if not names or '' in d['names'] or len(names) > 2:
        continue
    cand[addr] = (names, d['size'])
count = collections.Counter(tuple(n) for n, s in cand.values())
for addr, (names, size) in sorted(cand.items()):
    base = ('LuaEvent_' + '_'.join(clean(n) for n in names) + '_Raise') if len(names) == 1 else ('LuaEvents_' + '_'.join(clean(n) for n in names) + '_Raise')
    name = base if count[tuple(names)] == 1 else base + '_' + addr
    note = '' if size <= 1000 else ' The function is %d bytes and does other work as well.' % size
    out[addr] = (name,
                 'Raises Lua event %s through ClientEvent_DispatchNamedEvent @ %s; automatic string-anchor pass.%s' % (' and '.join(names), helper, note),
                 'lua-event')

rows = []
skipped = 0
for addr, (name, comment, rule) in sorted(out.items()):
    if addr.lower() in existing:
        skipped += 1
        continue
    assert ',' not in comment and '"' not in comment and ',' not in name, (name, comment)
    rows.append((addr, name, comment, rule))
print('rows', len(rows), 'skipped (already labelled)', skipped, collections.Counter(r[3] for r in rows))
with open(r'I:\tools\string_labels_generated.csv', 'w', encoding='utf-8', newline='') as f:
    for addr, name, comment, rule in rows:
        f.write('WildStar64.exe,%s,%s,%s' % (addr, name, comment) + chr(13) + chr(10))
dups = [n for n, c in collections.Counter(r[1] for r in rows).items() if c > 1]
print('duplicate names', len(dups), dups[:5])
replace = sorted(a for a, *_ in rows if a.lower() in placeholder)
open(r'I:\tools\string_labels_replace.txt', 'w').write('\n'.join(replace))
print('placeholder FUN_ rows to replace', len(replace))
