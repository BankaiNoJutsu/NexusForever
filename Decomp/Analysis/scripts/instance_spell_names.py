import struct, sys, re
sys.stdout.reconfigure(encoding='utf-8')
src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('e, rse, ne, be')[0]
exec(src)
e, rse, ne, be = table('Spell4Effects')
effs = {}
for i in range(ne):
    r = struct.unpack_from('<%dI' % (rse//4), e, be + i*rse)
    effs.setdefault(r[1], []).append((r[3], r[5], r[6], r[7]))
pat = re.compile(sys.argv[1], re.I)
for sp, bid in sorted(base_of.items()):
    n = spname(sp)
    if n and pat.search(n):
        print(sp, '"%s"' % n, 'effects(type,delay,tick,dur):', [(hex(a),b,c,d) for a,b,c,d in effs.get(sp, [])][:4])
