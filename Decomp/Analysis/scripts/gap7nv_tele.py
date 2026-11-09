"""Gap 7 new-video pass: list Spell4 rows whose en-US name matches a regex, with their Spell4Telegraph -> TelegraphDamage rows (read-only).
Usage: py -3.13 -I gap7nv_tele.py "<regex>" [shape-filter]"""
import struct, sys, re
sys.stdout.reconfigure(encoding='utf-8')
src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split("e, rse, ne, be")[0]
exec(src)
pat = re.compile(sys.argv[1], re.I)
shp = int(sys.argv[2]) if len(sys.argv) > 2 else None
st, rst, nst, bst = table('Spell4Telegraph'); td, rtd, ntd, btd = table('TelegraphDamage')
tdrow = {}
for i in range(ntd):
    r = struct.unpack_from('<%dI' % (rtd//4), td, btd + i*rtd)
    tdrow[r[0]] = r
f = lambda x: round(struct.unpack('<f', struct.pack('<I', x))[0], 3)
bys = {}
for i in range(nst):
    r = struct.unpack_from('<III', st, bst + i*rst); bys.setdefault(r[1], []).append(r[2])
for sp in sorted(base_of):
    n = spname(sp)
    if n and pat.search(n) and sp in bys:
        rows = [tdrow[t] for t in bys[sp] if t in tdrow and (shp is None or tdrow[t][2] == shp)]
        if not rows: continue
        print(f'spell {sp} "{n}"')
        for r in rows:
            print(f'   td={r[0]} sub={r[1]} shape={r[2]} P={[f(x) for x in r[3:9]]} t={r[9]}-{r[10]} ramp={r[11]}/{r[12]} off=({f(r[13])},{f(r[14])},{f(r[15])}) rot={f(r[16])} flags={r[17]:#x}')
