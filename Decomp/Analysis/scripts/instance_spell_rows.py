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
        # Raw row layout (verified 2026-11-09 against Tempest 63114 CastTime 15000): r[2] description string offset,
        # r[4] Spell4BaseIdBaseSpell, r[5] TierIndex, r[6] RavelInstanceId, r[7] CastTime (ms).
        print(r[0], repr(nm), '| base', r[4], 'tier', r[5], 'castTimeMs', r[7])
