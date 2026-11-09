import struct, sys, re
sys.stdout.reconfigure(encoding='utf-8')
t = open(r'I:\tools\extract_en\en-US.bin', 'rb').read()
tl, to, sl, so, ll, lo, rc, ro, stl, sto = struct.unpack_from('<10Q', t, 16)
recs = struct.unpack_from('<' + 'II' * rc, t, 96 + ro)
tx = dict(zip(recs[0::2], recs[1::2])); sb = 96 + sto
def text(i):
    p = sb + tx[i] * 2; q = p
    while q + 2 <= len(t) and t[q:q + 2] != b'\x00\x00' and q - p < 4000: q += 2
    return t[p:q].decode('utf-16le')
pat = re.compile(sys.argv[1], re.I)
for i in sorted(tx):
    s = text(i)
    if pat.search(s): print(i, repr(s[:150]))
if len(sys.argv) > 3:
    for i in range(int(sys.argv[2]), int(sys.argv[3])):
        if i in tx: print(i, repr(text(i)[:150]))
