import re, sys, csv, collections
sys.stdout.reconfigure(encoding='utf-8')

# parse runs
runs = []
cur = None
for l in open(r'I:\tools\pair_runs.txt', encoding='utf-8'):
    if l.startswith('RUN'):
        p = l.split()
        cur = {'start': p[1], 'entries': []}
        runs.append(cur)
    else:
        p = l.split()
        cur['entries'].append((p[0], p[1].split('=')[0]))

# parse owners
owners = {}
cur = None
for l in open(r'I:\tools\run_owners.txt', encoding='utf-8'):
    l = l.rstrip('\n')
    if l.startswith('RUN '):
        cur = l.split()[1]
        owners[cur] = []
    elif l.startswith('  ref from'):
        m = re.match(r'  ref from (\w+) in (\w+)', l)
        if m and m.group(2) != '<none>':
            owners[cur].append({'registrar': m.group(2), 'strings': []})
    elif l.startswith('    strings:') and owners[cur]:
        owners[cur][-1]['strings'] = [x for x in l.split('strings:')[1].strip()[1:-1].split(', ') if x]

SKIP = {'__index', '__gc', '__newindex', '__mode', 'New'}

def clean(s):
    return re.sub(r'[^A-Za-z0-9]+', '_', s).strip('_')

def classify(strings):
    cands = [s for s in strings if re.fullmatch(r'[A-Za-z][A-Za-z0-9_.]*', s) and s not in SKIP]
    if not cands:
        return None, False
    first = cands[0]
    confident = first.startswith(('Game.', 'Client.', 'Apollo.')) or first.endswith('Lib') or ('__index' in strings and strings.index('__index') <= 2)
    return first, confident

labels = {}
stats = collections.Counter()
for r in runs:
    own = owners.get(r['start'], [])
    cls, conf, reg = None, False, None
    if own:
        reg = own[0]['registrar']
        cls, conf = classify(own[0]['strings'])
        # ambiguity: different registrars/classes across refs
        others = {classify(o['strings'])[0] for o in own if classify(o['strings'])[0]}
        if len(others) > 1:
            conf = False
    stats['runs'] += 1
    if cls and conf:
        stats['classified'] += 1
        prefix = 'Lua_' + clean(cls.replace('Game.', '', 1) if cls.startswith('Game.') else cls)
    else:
        prefix = 'Lua_T' + r['start']
        stats['unclassified'] += 1
    for name, fn in r['entries']:
        if not re.fullmatch(r'[A-Za-z_][A-Za-z0-9_]*', name):
            continue
        label = prefix + '_' + name
        comment = 'Lua method table %s entry %s%s; automatic table-scan labelling pass; %s.' % (
            r['start'], name, (' (registrar %s; class string %s)' % (reg, cls)) if cls else '', 'class from registrar strings' if (cls and conf) else 'class not resolved')
        if fn in labels:
            labels[fn]['aliases'].append(label)
            continue
        labels[fn] = {'name': label, 'comment': comment, 'aliases': []}
        stats['functions'] += 1
print(dict(stats))

# existing labels
existing = {}
for row in csv.reader(open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\function_labels.csv', encoding='utf-8')):
    if not row or row[0].startswith('#') or row[0] == 'program':
        continue
    existing[(row[0], row[1].lower())] = row[2]
skipped = [a for a in labels if ('WildStar64.exe', a.lower()) in existing]
print('already labelled (kept):', len(skipped))
conflicts = [(a, existing[('WildStar64.exe', a.lower())], labels[a]['name']) for a in skipped]
for c in conflicts[:12]:
    print('  existing', c)
with open(r'I:\tools\lua_labels_generated.csv', 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f)
    for a, v in sorted(labels.items()):
        if ('WildStar64.exe', a.lower()) in existing:
            continue
        c = v['comment'] + ((' Also bound as ' + ' / '.join(v['aliases']) + '.') if v['aliases'] else '')
        assert ',' not in c and '"' not in c
        f.write('WildStar64.exe,%s,%s,%s' % (a, v['name'], c) + chr(13) + chr(10))
print('new rows', len(labels) - len(skipped))
