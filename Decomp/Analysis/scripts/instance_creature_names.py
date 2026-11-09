import struct, sys
sys.stdout.reconfigure(encoding='utf-8')
src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('def table')[0]
exec(src)
def table(name):
    d = open(r'I:\tools\extract\DB\%s.tbl' % name, 'rb').read()
    return d, struct.unpack_from('<Q', d, 24)[0], struct.unpack_from('<Q', d, 48)[0], 0x60 + struct.unpack_from('<Q', d, 64)[0]
d, rs, n, b = table('Creature2')
want = {int(a) for a in sys.argv[1:]}
for i in range(n):
    r = struct.unpack_from('<%dI' % (rs // 4), d, b + i * rs)
    if r[0] in want:
        # find any word that is a text id with a name
        names = [(j, text(x)) for j, x in enumerate(r[:40]) if x in tx and text(x) and len(text(x)) < 60]
        print(r[0], names[:3])
