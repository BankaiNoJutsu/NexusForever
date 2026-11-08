import struct, glob, os, collections, sys
sys.stdout.reconfigure(encoding='utf-8')
DB = r'I:\tools\extract\DB'
def load(name):
    d = open(os.path.join(DB, name + '.tbl'), 'rb').read()
    rs = struct.unpack_from('<Q', d, 24)[0]; n = struct.unpack_from('<Q', d, 48)[0]; ro = struct.unpack_from('<Q', d, 64)[0]
    b = 0x60 + ro
    return [struct.unpack_from('<%dI' % (rs // 4), d, b + i * rs) for i in range(n)]
pm = load('PathMission')
by = collections.defaultdict(list)
for r in pm:
    by[r[3]].append(r[5])
print({t: len(v) for t, v in sorted(by.items())})
cands = [os.path.basename(f)[:-4] for f in glob.glob(os.path.join(DB, 'Path*.tbl'))]
for t in (13, 14, 5, 6, 17):
    objs = set(by[t])
    best = []
    for c in cands:
        try:
            ids = {r[0] for r in load(c)}
        except Exception:
            continue
        hit = len(objs & ids)
        if hit: best.append((hit, c))
    best.sort(reverse=True)
    print('type', t, 'missions', len(by[t]), 'distinct objectIds', len(objs), '->', best[:4])
