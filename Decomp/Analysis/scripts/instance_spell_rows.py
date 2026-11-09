import struct, sys
sys.stdout.reconfigure(encoding='utf-8')
src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('e, rse, ne, be')[0]
exec(src)
lo, hi = int(sys.argv[1]), int(sys.argv[2])
def s(d, off):
    p = 0x60 + off; q = p
    while q+2 <= len(d) and d[q:q+2] != b'\0\0': q += 2
    return d[p:q].decode('utf-16le', 'replace')
for i in range(n4):
    base = b4 + i*rs4
    r = struct.unpack_from('<%dI' % (rs4//4), s4, base)
    if lo <= r[0] <= hi:
        nm = spname(r[0])
        try: desc = s(s4, r[1])
        except Exception: desc = '?'
        print(r[0], repr(nm), '| desc:', desc[:60], '| cast', r[5], 'dur', r[6], 'cd', r[7], 'rng', [round(struct.unpack('<f', struct.pack('<I', x))[0],1) for x in r[8:11]])
