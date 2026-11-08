import re, collections, sys
sys.stdout.reconfigure(encoding='utf-8')
exec(open(r'I:\tools\impact_2425.py', encoding='utf-8').read().split("refs = collections.defaultdict(set)")[0])
provided = collections.Counter()
for l in open(r'I:\tools\cp_rows.txt', encoding='utf-8').read().splitlines()[5:]:
    d = l.split('|')[1].split()[:2]
    for k, x in enumerate(d):
        v = int(x, 16) if x.startswith('0x') else int(x)
        for b in range(32):
            if v >> b & 1: provided[(k, b)] += 1
byte = lambda w: sorted({b for (k, b), n in provided.items() if k == w})
print('bits any ChangePhase row adds in DataBits00 (IPerceive):', byte(0))
print('bits any ChangePhase row adds in DataBits01 (PerceiveMe):', byte(1))
need = collections.defaultdict(list)
for l in open(r'I:\tools\prereq_refs_detail.tsv', encoding='utf-8'):
    t, tab, col, eid, pid = l.rstrip('\n').split('\t')
    t, eid, pid = int(t), int(eid), int(pid)
    if t in (24, 25) and (tab, col) in SERVER:
        for (st, c, o, v) in vals[pid]:
            if st == t and c == 1 and v != 0:
                need[(t, v & 31)].append((tab, col, eid, pid))
print()
print('server-evaluated Equal slots needing a non-zero bit: (type, bit) -> count, provided by some ChangePhase row (DataBits00)?')
for (t, b), lst in sorted(need.items()):
    print('  type %d bit %2d: %2d refs  provided=%s  e.g. %s' % (t, b, len(lst), (0, b) in provided, lst[:2]))
print('total', sum(len(v) for v in need.values()), 'unprovided', sum(len(v) for (t, b), v in need.items() if (0, b) not in provided))
