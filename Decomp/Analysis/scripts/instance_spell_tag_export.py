import struct, sys, re, csv, collections
sys.stdout.reconfigure(encoding='utf-8')
src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('e, rse, ne, be')[0]
exec(src)
def desc(d, off):
    p = 0x60 + off
    while p - 2 >= 0 and d[p - 2:p] != b'\x00\x00': p -= 2
    q = p
    while q + 2 <= len(d) and d[q:q + 2] != b'\x00\x00': q += 2
    return d[p:q].decode('utf-16le', 'replace')
rows = []
for i in range(n4):
    r = struct.unpack_from('<%dI' % (rs4 // 4), s4, b4 + i * rs4)
    if not r[2]: continue
    t = desc(s4, r[2])
    m = re.match(r'^((?:\[[A-Za-z0-9 _\-]+\])+)\s*(e\d+|w\d+)?', t)
    if m:
        rows.append((r[0], m.group(1), m.group(2) or '', t, r[4], r[5], r[7]))
out = sys.argv[1]
with open(out, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f); w.writerow(['spell4_id', 'tag', 'encounter', 'description', 'base_spell_id', 'tier', 'cast_time_ms'])
    for r in rows: w.writerow(r)
c = collections.Counter((r[1], r[2]) for r in rows)
bytag = collections.Counter(r[1] for r in rows)
print('tagged rows', len(rows))
for k, v in bytag.most_common(60): print(k, v)
