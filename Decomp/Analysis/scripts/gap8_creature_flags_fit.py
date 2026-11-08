"""Gap 8: fit ModifyCreatureFlags (0x3C) data words against Creature2.Flags / UiFlags bit usage (read-only)."""
import struct, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, r'I:\tools')
import colmap
def tbl(name):
    d = open(r'I:\tools\extract\DB\%s.tbl' % name, 'rb').read()
    return d, struct.unpack_from('<Q', d, 24)[0], struct.unpack_from('<Q', d, 48)[0], 0x60 + struct.unpack_from('<Q', d, 64)[0]
d, rs, n, b = tbl('Spell4Effects')
rows = [struct.unpack_from('<%dI' % (rs//4), d, b+i*rs) for i in range(n)]
rows = [r for r in rows if r[3] == 0x3C]
print('rows', len(rows))
print('DB00 values', collections.Counter(r[9] for r in rows))
print('DB02 values', collections.Counter(r[11] for r in rows))
print('DB04..09 nonzero rows', sum(1 for r in rows if any(r[13:19])))
print('Flags(row) values', collections.Counter(r[8] for r in rows))
def bits(v): return [i for i in range(32) if v >> i & 1]
A = collections.Counter(); B = collections.Counter()
for r in rows:
    if r[10]:
        for i in bits(r[10]): A[i] += 1
    if r[12]:
        for i in bits(r[12]): B[i] += 1
print('DB01 mask bit usage', sorted(A.items()))
print('DB03 mask bit usage', sorted(B.items()))
cols, w = colmap.columns('Creature2'); inv = {nm: i for i, nm in cols}
d2, rs2, n2, b2 = tbl('Creature2')
fl = collections.Counter(); ui = collections.Counter()
for i in range(n2):
    fl[struct.unpack_from('<I', d2, b2+i*rs2+inv['Flags']*4)[0]] += 1
    ui[struct.unpack_from('<I', d2, b2+i*rs2+inv['UiFlags']*4)[0]] += 1
def attested(c):
    s = set()
    for v in c:
        s.update(bits(v))
    return s
fa, ua = attested(fl), attested(ui)
print('Creature2.Flags bits attested', sorted(fa)); print('Creature2.UiFlags bits attested', sorted(ua))
print('DB01 bits not in Flags', sorted(set(A) - fa), '| not in UiFlags', sorted(set(A) - ua))
print('DB03 bits not in UiFlags', sorted(set(B) - ua), '| not in Flags', sorted(set(B) - fa))
# whole-value matches: does the effect mask value appear as a Creature2 whole value?
print('DB01 masks that are whole Flags values:', sorted({hex(r[10]) for r in rows if r[10] and r[10] in fl}))
print('DB03 masks that are whole UiFlags values:', sorted({hex(r[12]) for r in rows if r[12] and r[12] in ui}))
print('DB03 masks that are whole Flags values:', sorted({hex(r[12]) for r in rows if r[12] and r[12] in fl}))
print('DB01 masks that are whole UiFlags values:', sorted({hex(r[10]) for r in rows if r[10] and r[10] in ui}))
