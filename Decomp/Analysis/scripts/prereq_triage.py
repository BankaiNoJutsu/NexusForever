import re, subprocess, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
out = subprocess.run([sys.executable, '-I', r'I:\tools\prereq_census.py'], capture_output=True, text=True, encoding='utf-8').stdout
cur = {}
for l in out.splitlines()[2:]:
    m = re.match(r'\s*(\d+)\s+(\S+)\s+slots=(\d+)', l)
    if m: cur[int(m.group(1))] = (m.group(2), int(m.group(3)))
prior = {}
for l in open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\PREREQUISITE_REMAINING_TYPE_GATES_20261107.md', encoding='utf-8'):
    m = re.match(r'\|\s*(\d+)\s*\|\s*(\S+)\s*\|\s*(\d+)\s*\|\s*([^|]+)\|\s*([^|]*)\|', l)
    if m: prior[int(m.group(1))] = (m.group(4).strip(), m.group(5).strip())
cls = collections.defaultdict(list)
for t, (nm, s) in cur.items():
    body = prior.get(t, ('not in the 2026-11-07 table', ''))[0]
    c = 'client stub' if body.startswith('stub') else 'no evaluator case' if body.startswith('no evaluator') else 'real client body or inline' if body in ('inline',) or body.startswith(('vtable', 'inline')) else body
    cls[c].append((s, t, nm))
for c, v in cls.items():
    v.sort(reverse=True)
    print('==', c, 'types', len(v), 'slots', sum(s for s, _, _ in v))
    print('   ', ', '.join('%d %s (%d)' % (t, nm, s) for s, t, nm in v))
gone = sorted(set(prior) - set(cur))
print('implemented since the 2026-11-07 table:', gone)
print('in the census but not in that table:', sorted(set(cur) - set(prior)))
