import struct, sys, re
sys.stdout.reconfigure(encoding='utf-8')
src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('e, rse, ne, be')[0]
exec(src)


def desc(d, off):
    p = 0x60 + off
    while p - 2 >= 0 and d[p - 2:p] != b'\x00\x00':
        p -= 2
    q = p
    while q + 2 <= len(d) and d[q:q + 2] != b'\x00\x00':
        q += 2
    return d[p:q].decode('utf-16le', 'replace')


pat = re.compile(sys.argv[1])
n = 0
for i in range(n4):
    r = struct.unpack_from('<%dI' % (rs4 // 4), s4, b4 + i * rs4)
    if not r[2]:
        continue
    t = desc(s4, r[2])
    if pat.search(t):
        print(r[0], '|', t[:110], '| tier', r[5], 'castMs', r[7])
        n += 1
print('n', n)
