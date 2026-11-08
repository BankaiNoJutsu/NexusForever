"""Gap 8: dump Spell4Effects rows for effect types 0x18, 0x3C, 0x6D with spell names (read-only)."""
import struct, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
t = open(r'I:\tools\extract_en\en-US.bin', 'rb').read()
tl, to, sl, so, ll, lo, rc, ro, stl, sto = struct.unpack_from('<10Q', t, 16)
recs = struct.unpack_from('<' + 'II' * rc, t, 96 + ro)
tx = dict(zip(recs[0::2], recs[1::2])); sb = 96 + sto
def text(i):
    if i not in tx: return None
    p = sb + tx[i] * 2; q = p
    while q + 2 <= len(t) and t[q:q + 2] != b'\x00\x00' and q - p < 4000: q += 2
    return t[p:q].decode('utf-16le')
def table(name):
    d = open(r'I:\tools\extract\DB\%s.tbl' % name, 'rb').read()
    return d, struct.unpack_from('<Q', d, 24)[0], struct.unpack_from('<Q', d, 48)[0], 0x60 + struct.unpack_from('<Q', d, 64)[0]
s4, rs4, n4, b4 = table('Spell4'); sbd, rsb, nb, bb = table('Spell4Base')
base_of = {struct.unpack_from('<I', s4, b4+i*rs4)[0]: struct.unpack_from('<I', s4, b4+i*rs4+16)[0] for i in range(n4)}
name_of = {}
for i in range(nb):
    bid, tid = struct.unpack_from('<II', sbd, bb+i*rsb); name_of[bid] = tid
def spname(sp): return text(name_of.get(base_of.get(sp, 0), 0))
e, rse, ne, be = table('Spell4Effects')
want = {int(x, 0) for x in sys.argv[1:]} or {0x18, 0x3C, 0x6D}
out = collections.defaultdict(list)
for i in range(ne):
    r = struct.unpack_from('<%dI' % (rse//4), e, be + i*rse)
    # id, spell, targetflags, type, dmgtype, delay, tick, duration, flags, db00..db09 (9..18)
    if r[3] in want: out[r[3]].append(r)
for ty, rows in out.items():
    print('=== type 0x%X rows %d' % (ty, len(rows)))
    for r in rows:
        f = struct.unpack('<f', struct.pack('<I', r[9]))[0]
        print('id=%d spell=%d "%s" tf=%d delay=%d tick=%d dur=%d fl=%d db=%s prq=%s param=%s/%s' % (
            r[0], r[1], spname(r[1]), r[2], r[5], r[6], r[7], r[8], [hex(x) if x>9999 else x for x in r[9:19]],
            r[29:34], r[34:38] if False else '', [round(struct.unpack('<f', struct.pack('<I', x))[0],3) for x in r[38:42]]))
